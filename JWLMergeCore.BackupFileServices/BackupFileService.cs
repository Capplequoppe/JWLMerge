namespace JWLMergeCore.BackupFileServices
{
    using System.Threading.Tasks;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.IO.Compression;
    using System.Linq;
    using System.Security.Cryptography;
    using System.Text;
    using Events;
    using Exceptions;
    using Helpers;
    using Models;
    using Models.DatabaseModels;
    using Models.ManifestFile;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;
    using Newtonsoft.Json.Serialization;
    using Microsoft.Extensions.Logging;

    public sealed class BackupFileService : IBackupFileService
    {
        private const int ManifestVersionSupported = 1;
        private const int DatabaseVersionSupported = 8;
        private const string ManifestEntryName = "manifest.json";
        private const string DatabaseEntryName = "userData.db";

        private readonly Merger _merger;
        private readonly ILogger? _logger;
        public BackupFileService(ILogger? logger = null)
        {
            _logger = logger;
            _merger = new Merger(logger);
            _merger.ProgressEvent += MergerProgressEvent;
            ProgressEvent += BackupFileService_ProgressEvent;
            
        }

        private static void BackupFileService_ProgressEvent(object? sender, ProgressEventArgs e)
        {
        }

        public event EventHandler<ProgressEventArgs> ProgressEvent;

        /// <inheritdoc />
        public async Task<BackupFile> Load(string backupFilePath)
        {
            if (string.IsNullOrEmpty(backupFilePath))
            {
                throw new ArgumentNullException(nameof(backupFilePath));
            }

            if (!File.Exists(backupFilePath))
            {
                throw new BackupFileServicesException($"File does not exist: {backupFilePath}");
            }

            var filename = Path.GetFileName(backupFilePath);
            ProgressMessage($"Loading {filename}");

            using var archive = new ZipArchive(File.OpenRead(backupFilePath), ZipArchiveMode.Read);
            var manifest = ReadManifest(filename, archive);

            var database = await ReadDatabase(archive, manifest.UserDataBackup.DatabaseName);

            return new BackupFile
            {
                Manifest = manifest,
                Database = database,
            };
        }

        /// <inheritdoc />
        public BackupFile CreateBlank()
        {
            ProgressMessage("Creating blank file");

            var database = new Database(_logger);
            database.InitBlank();

            return new BackupFile
            {
                Manifest = new Manifest(),
                Database = database,
            };
        }

        /// <inheritdoc />
        public async Task WriteNewDatabase(
            BackupFile backup,
            string newDatabaseFilePath,
            string originalJwLibraryFilePathForSchema)
        {
            if (backup == null)
            {
                throw new ArgumentNullException(nameof(backup));
            }

            ProgressMessage("Writing merged database file");

            await using var memoryStream = new MemoryStream();
            using (var archive = new ZipArchive(memoryStream, ZipArchiveMode.Create, true))
            {
                _logger?.LogDebug("Created ZipArchive");

                var tmpDatabaseFileName = ExtractDatabaseToFile(originalJwLibraryFilePathForSchema);
                try
                {
                    backup.Manifest.UserDataBackup.Hash = GenerateDatabaseHash(tmpDatabaseFileName);

                    var manifestEntry = archive.CreateEntry(ManifestEntryName);
#pragma warning disable S3966 // Objects should not be disposed more than once
                    await using (var entryStream = manifestEntry.Open())
#pragma warning restore S3966 // Objects should not be disposed more than once
                    await using (var streamWriter = new StreamWriter(entryStream))
                    {
                        await streamWriter.WriteAsync(
                            JsonConvert.SerializeObject(
                                backup.Manifest,
                                new JsonSerializerSettings
                                {
                                    ContractResolver = new CamelCasePropertyNamesContractResolver(),
                                }));
                    }

                    await AddDatabaseEntryToArchive(archive, backup.Database, tmpDatabaseFileName);
                }
                finally
                {
                    _logger?.LogDebug("Deleting {tmpDatabaseFileName}", tmpDatabaseFileName);
                    File.Delete(tmpDatabaseFileName);
                }
            }

            await using var fileStream = new FileStream(newDatabaseFilePath, FileMode.Create);
            ProgressMessage("Finishing");

            memoryStream.Seek(0, SeekOrigin.Begin);
            await memoryStream.CopyToAsync(fileStream);
        }

        /// <inheritdoc />
        public int RemoveTags(Database database)
        {
            if (database == null)
            {
                throw new ArgumentNullException(nameof(database));
            }

            // clear all but the first tag (which will be the "favorites")...
            var tagCount = database.Tags.Count;
            if (tagCount > 2)
            {
                database.Tags.RemoveRange(1, tagCount - 1);
            }

            database.TagMaps.Clear();

            return tagCount > 1
                ? tagCount - 1
                : tagCount;
        }

        /// <inheritdoc />
        public int RemoveBookmarks(Database database)
        {
            if (database == null)
            {
                throw new ArgumentNullException(nameof(database));
            }

            var count = database.Bookmarks.Count;
            database.Bookmarks.Clear();
            return count;
        }

        /// <inheritdoc />
        public int RemoveNotes(Database database)
        {
            if (database == null)
            {
                throw new ArgumentNullException(nameof(database));
            }

            var count = database.Notes.Count;
            database.Notes.Clear();
            return count;
        }

        /// <inheritdoc />
        public int RemoveUnderlining(Database database)
        {
            if (database == null)
            {
                throw new ArgumentNullException(nameof(database));
            }

            if (!database.Notes.Any())
            {
                var count = database.UserMarks.Count;
                database.UserMarks.Clear();
                return count;
            }

            // we must retain user marks that are associated with notes...
            var userMarksToRetain = new HashSet<int>();
            foreach (var note in database.Notes.Where(note => note.UserMarkId != null))
            {
                userMarksToRetain.Add(note.UserMarkId!.Value);
            }

            var countRemoved = 0;
            foreach (var userMark in Enumerable.Reverse(database.UserMarks))
            {
                if (!userMarksToRetain.Contains(userMark.UserMarkId))
                {
                    database.UserMarks.Remove(userMark);
                    ++countRemoved;
                }
            }

            return countRemoved;
        }

        /// <inheritdoc />
        public BackupFile Merge(IReadOnlyCollection<BackupFile> files)
        {
            if (files == null)
            {
                throw new ArgumentNullException(nameof(files));
            }

            ProgressMessage($"Merging {files.Count} backup files");

            var fileNumber = 1;
            foreach (var file in files)
            {
                _logger?.LogDebug("Merging backup file {fileNumber} = {fileName}", fileNumber++, file.Manifest.Name);
                _logger?.LogDebug("===================");

                Clean(file);
            }

            // just pick the first manifest as the basis for the 
            // manifest in the final merged file...
            var newManifest = UpdateManifest(files.First().Manifest);

            var mergedDatabase = MergeDatabases(files);
            return new BackupFile { Manifest = newManifest, Database = mergedDatabase };
        }

        /// <inheritdoc />
        public async Task<BackupFile> Merge(IReadOnlyCollection<string> files)
        {
            if (files == null)
            {
                throw new ArgumentNullException(nameof(files));
            }

            ProgressMessage($"Merging {files.Count} backup files");

            var fileNumber = 1;
            var originals = new List<BackupFile>();
            foreach (var file in files)
            {
                _logger?.LogDebug("Merging file {fileNumber} = {fileName}", fileNumber++, file);
                _logger?.LogDebug("============");

                var backupFile = await Load(file);
                Clean(backupFile);
                originals.Add(backupFile);
            }

            // just pick the first manifest as the basis for the 
            // manifest in the final merged file...
            var newManifest = UpdateManifest(originals.First().Manifest);

            var mergedDatabase = MergeDatabases(originals);
            return new BackupFile { Manifest = newManifest, Database = mergedDatabase };
        }

        /// <inheritdoc />
        public BackupFile ImportBibleNotes(
            BackupFile originalBackupFile,
            IEnumerable<BibleNote> notes,
            string bibleKeySymbol,
            int mepsLanguageId,
            ImportBibleNotesParams options)
        {
            if (originalBackupFile == null)
            {
                throw new ArgumentNullException(nameof(originalBackupFile));
            }

            if (notes == null)
            {
                throw new ArgumentNullException(nameof(notes));
            }

            ProgressMessage("Importing Bible notes");

            var newManifest = UpdateManifest(originalBackupFile.Manifest);
            var notesImporter = new NotesImporter(
                originalBackupFile.Database,
                bibleKeySymbol,
                mepsLanguageId,
                options);

            notesImporter.Import(notes);

            return new BackupFile { Manifest = newManifest, Database = originalBackupFile.Database };
        }

        private static bool SupportDatabaseVersion(int version) => version == DatabaseVersionSupported;

        private static bool SupportManifestVersion(int version) => version == ManifestVersionSupported;

        private Manifest UpdateManifest(Manifest manifestToBaseOn)
        {
            _logger?.LogDebug("Updating manifest");

            var result = manifestToBaseOn.Clone();

            var now = DateTime.Now;
            var simpleDateString = $"{now.Year}-{now.Month:D2}-{now.Day:D2}";

            result.Name = $"merged_{simpleDateString}";
            result.CreationDate = simpleDateString;
            result.UserDataBackup.DeviceName = "JWLMerge";
            result.UserDataBackup.DatabaseName = DatabaseEntryName;

            _logger?.LogDebug("Updated manifest");

            return result;
        }

        private Database MergeDatabases(IEnumerable<BackupFile> jwLibraryFiles)
        {
            ProgressMessage("Merging databases");
            return _merger.Merge(jwLibraryFiles.Select(x => x.Database));
        }

        private void MergerProgressEvent(object? sender, ProgressEventArgs? e)
        {
            OnProgressEvent(e);
        }

        private void Clean(BackupFile backupFile)
        {
            _logger?.LogDebug("Cleaning backup file {backupFile}", backupFile.Manifest.Name);

            var cleaner = new Cleaner(backupFile.Database,_logger);
            var rowsRemoved = cleaner.Clean();
            if (rowsRemoved > 0)
            {
                ProgressMessage($"Removed {rowsRemoved} inaccessible rows");
            }
        }

        private async Task<Database> ReadDatabase(ZipArchive archive, string databaseName)
        {
            ProgressMessage($"Reading database {databaseName}");

            var databaseEntry = archive.Entries.FirstOrDefault(x => x.Name.Equals(databaseName, StringComparison.OrdinalIgnoreCase));
            if (databaseEntry == null)
            {
                throw new BackupFileServicesException("Could not find database entry in jwLibrary file");
            }

            Database result;
            var tmpFile = Path.GetTempFileName();
            try
            {
                _logger?.LogDebug("Extracting database to {tmpFile}", tmpFile);
                databaseEntry.ExtractToFile(tmpFile, overwrite: true);

                var dataAccessLayer = new DataAccessLayer(tmpFile, _logger);
                result = await dataAccessLayer.ReadDatabase();
            }
            finally
            {
                _logger?.LogDebug("Deleting {tmpFile}", tmpFile);
                File.Delete(tmpFile);
            }

            return result;
        }

        private string ExtractDatabaseToFile(string jwLibraryFile)
        {
            _logger?.LogDebug("Opening ZipArchive {jwLibraryFile}", jwLibraryFile);

            using var archive = new ZipArchive(File.OpenRead(jwLibraryFile), ZipArchiveMode.Read);
            var manifest = ReadManifest(Path.GetFileName(jwLibraryFile), archive);

            var databaseEntry = archive.Entries.FirstOrDefault(x => x.Name.Equals(manifest.UserDataBackup.DatabaseName, StringComparison.OrdinalIgnoreCase));
            var tmpFile = Path.GetTempFileName();
            databaseEntry.ExtractToFile(tmpFile, overwrite: true);

            _logger?.LogInformation("Created temp file: {tmpDatabaseFileName}", tmpFile);
            return tmpFile;
        }

        private Manifest ReadManifest(string filename, ZipArchive archive)
        {
            ProgressMessage("Reading manifest");

            var manifestEntry = archive.Entries.FirstOrDefault(x => x.Name.Equals(ManifestEntryName, StringComparison.OrdinalIgnoreCase));
            if (manifestEntry == null)
            {
                throw new BackupFileServicesException($"Could not find manifest entry in jwlibrary file: {filename}");
            }

            using var stream = new StreamReader(manifestEntry.Open());
            var fileContents = stream.ReadToEnd();

            _logger?.LogDebug("Parsing manifest");
            dynamic data = JObject.Parse(fileContents);

            int manifestVersion = data.version ?? 0;
            if (!SupportManifestVersion(manifestVersion))
            {
                throw new WrongManifestVersionException(filename, ManifestVersionSupported, manifestVersion);
            }

            int databaseVersion = data.userDataBackup.schemaVersion ?? 0;
            if (!SupportDatabaseVersion(databaseVersion))
            {
                throw new WrongDatabaseVersionException(filename, DatabaseVersionSupported, databaseVersion);
            }

            var result = JsonConvert.DeserializeObject<Manifest>(fileContents);

            var prettyJson = JsonConvert.SerializeObject(result, Formatting.Indented);
            _logger?.LogDebug("Parsed manifest {manifestJson}", prettyJson);

            return result;
        }

        /// <summary>
        /// Generates the sha256 database hash that is required in the manifest.json file.
        /// </summary>
        /// <param name="databaseFilePath">
        /// The database file path.
        /// </param>
        /// <returns>The hash.</returns>
        private string GenerateDatabaseHash(string databaseFilePath)
        {
            ProgressMessage("Generating database hash");

            using var fs = new FileStream(databaseFilePath, FileMode.Open);
            using var bs = new BufferedStream(fs);
            using var sha1 = new SHA256Managed();
            var hash = sha1.ComputeHash(bs);
            var sb = new StringBuilder(2 * hash.Length);
            foreach (var b in hash)
            {
                sb.Append($"{b:x2}");
            }

            return sb.ToString();
        }

        private async Task AddDatabaseEntryToArchive(
            ZipArchive archive,
            Database database,
            string originalDatabaseFilePathForSchema)
        {
            ProgressMessage("Adding database to archive");

            var tmpDatabaseFile = await CreateTemporaryDatabaseFile(database, originalDatabaseFilePathForSchema);
            try
            {
                archive.CreateEntryFromFile(tmpDatabaseFile, DatabaseEntryName);
            }
            finally
            {
                File.Delete(tmpDatabaseFile);
            }
        }

        private async Task<string> CreateTemporaryDatabaseFile(
            Database backupDatabase,
            string originalDatabaseFilePathForSchema)
        {
            var tmpFile = Path.GetTempFileName();

            _logger?.LogDebug("Creating temporary database file {tmpFile}", tmpFile);

            var tempContext = await new DataAccessLayer(originalDatabaseFilePathForSchema,_logger).CreateEmptyClone(tmpFile);
            await DataAccessLayer.PopulateTables(backupDatabase,tempContext);

            return tmpFile;
        }

        private void OnProgressEvent(ProgressEventArgs? e) => ProgressEvent?.Invoke(this, e ?? new ProgressEventArgs());

        private void OnProgressEvent(string? message) => OnProgressEvent(new ProgressEventArgs { Message = message ?? "" });

        private void ProgressMessage(string? logMessage)
        {
            _logger?.LogInformation(logMessage);
            OnProgressEvent(logMessage);
        }
    }
}