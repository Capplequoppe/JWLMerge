namespace JWLMergeCore.BackupFileServices.Helpers
{
    using System;
    using System.IO;
    using System.Threading.Tasks;
    using DataAccess;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.Extensions.Logging;
    using System.Linq;
    using Models.DatabaseModels;

    /// <summary>
    /// Isolates all data access to the SQLite database embedded in
    /// jwlibrary files.
    /// </summary>
    internal class DataAccessLayer
    {
        private readonly string _databaseFilePath;
        private readonly ILogger? _logger;
        public DataAccessLayer(string databaseFilePath, ILogger? logger = null)
        {
            _logger = logger;
            _databaseFilePath = databaseFilePath;
        }

        /// <summary>
        /// Creates a new empty database using the schema from the current database.
        /// </summary>
        /// <param name="cloneFilePath">The clone file path (the new database).</param>
        public async Task<BackupDbContext> CreateEmptyClone(string cloneFilePath)
        {
            _logger?.LogDebug(($"Creating empty clone: {cloneFilePath}"));

            if (File.Exists(cloneFilePath))
            {
                File.Delete(cloneFilePath);
            }
            File.Copy(_databaseFilePath, cloneFilePath);

            var destination = CreateConnection(cloneFilePath);

            await ClearData(destination);
            return destination;
        }

        /// <summary>
        /// Populates the current database using the specified data.
        /// </summary>
        /// <param name="dataToUse">The data to use.</param>
        public static async Task PopulateTables(Database dataToUse, BackupDbContext context)
        {
            try
            {
                context.AddRange(dataToUse.Locations);

                context.AddRange(dataToUse.Notes);

                context.AddRange(dataToUse.UserMarks);

                context.AddRange(dataToUse.Tags);

                context.AddRange(dataToUse.TagMaps);

                context.AddRange(dataToUse.BlockRanges);

                context.AddRange(dataToUse.Bookmarks);
                await context.SaveChangesAsync();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }

        }

        /// <summary>
        /// Reads the current database.
        /// </summary>
        /// <returns><see cref="Database"/></returns>
        public async Task<Database> ReadDatabase()
        {
            var result = new Database();

            await using var connection = CreateConnection();
            result.InitBlank();

            var lastModified = await connection.LastModified.Select(s => s.TimeLastModified).FirstOrDefaultAsync();
            result.LastModified.TimeLastModified = lastModified;

            result.Locations.AddRange(await connection.Locations.AsNoTracking().ToArrayAsync());
            result.Notes.AddRange(await connection.Notes.AsNoTracking().ToArrayAsync());
            result.Tags.AddRange(await connection.Tags.AsNoTracking().ToArrayAsync());
            result.TagMaps.AddRange(await connection.TagMaps.AsNoTracking().ToArrayAsync());
            result.BlockRanges.AddRange(await connection.BlockRanges.AsNoTracking().ToArrayAsync());
            result.Bookmarks.AddRange(await connection.Bookmarks.AsNoTracking().ToArrayAsync());
            result.UserMarks.AddRange(await connection.UserMarks.AsNoTracking().ToArrayAsync());

            // ensure bookmarks appear in similar order to original.
            result.Bookmarks.Sort((bookmark1, bookmark2) => bookmark1.Slot.CompareTo(bookmark2.Slot));

            return result;
        }

        private BackupDbContext CreateConnection() => CreateConnection(_databaseFilePath);

        private BackupDbContext CreateConnection(string filePath)
        {
            var connectionString = $"Data Source={filePath};";
            _logger?.LogDebug("SQL create connection: {connection}", connectionString);
            var optionsBuilder = new DbContextOptionsBuilder<BackupDbContext>();
            optionsBuilder.UseSqlite(connectionString);
            return new BackupDbContext(optionsBuilder.Options);
        }

        private static async Task ClearData(BackupDbContext context)
        {
            try
            {
                context.TagMaps.RemoveRange(await context.TagMaps.ToArrayAsync());

                context.UserMarks.RemoveRange(await context.UserMarks.ToArrayAsync());

                context.Tags.RemoveRange(await context.Tags.ToArrayAsync());

                context.Notes.RemoveRange(await context.Notes.ToArrayAsync());

                context.Locations.RemoveRange(await context.Locations.ToArrayAsync());

                context.Bookmarks.RemoveRange(await context.Bookmarks.ToArrayAsync());

                context.BlockRanges.RemoveRange(await context.BlockRanges.ToArrayAsync());
                await context.SaveChangesAsync();
                await UpdateLastModified(context);

                await VacuumDatabase(context);

            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }

        }

        private static Task VacuumDatabase(DbContext context) => context.Database.ExecuteSqlRawAsync("vacuum;");

        private static async Task UpdateLastModified(DbContext context)
        {
            await context.Database.ExecuteSqlRawAsync("delete from LastModified; insert into LastModified default values");
        }
    }
}