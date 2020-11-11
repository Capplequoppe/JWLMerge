namespace JWLMergeCore.AutoMerger
{
    using System;
    using System.Collections.Immutable;
    using System.IO;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using BackupFileServices;
    using BackupFileServices.Events;
    using Microsoft.Extensions.Hosting;
    using Microsoft.Extensions.Logging;

    public class Worker : BackgroundService
    {
        public event EventHandler<ProgressEventArgs> ProgressEvent;
        private readonly ILogger _logger;
        private readonly string _folderToWatch;
        public Worker()
        {
            _logger = LoggerFactory.Create(l =>
            {
                l.AddConsole();
            }).CreateLogger("Worker");
            _folderToWatch = Directory.GetCurrentDirectory();
            ProgressEvent += Worker_ProgressEvent;
        }

        private static void Worker_ProgressEvent(object? sender, ProgressEventArgs e)
        {
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var watcher = new FileSystemWatcher(_folderToWatch, "*.jwlibrary")
            {
                NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.CreationTime,
                EnableRaisingEvents = true
            };
            watcher.Created += (async (sender, e) =>
            {
                await File_Created(sender, e);
            });
            watcher.Renamed += (async (sender, e) =>
            {
                await File_Created(sender, e);
            });

            await File_Created(this, null);

            while (!stoppingToken.IsCancellationRequested)
            {
                _logger?.LogInformation("Worker running at: {time}", DateTimeOffset.Now);
                await Task.Delay(1000, stoppingToken);
            }
        }

        private async Task File_Created(object sender, FileSystemEventArgs? e)
        {
            if (e?.Name == "master.jwlibrary" || e?.Name == "master.old.jwlibrary") return;
            if (File.Exists($"{_folderToWatch}\\master.old.jwlibrary")) File.Delete($"{_folderToWatch}\\master.old.jwlibrary");
            var fileNames = Directory.EnumerateFiles(_folderToWatch, "*.jwlibrary").ToImmutableArray();
            if (fileNames.Length <= 1) return;

            if (File.Exists($"{_folderToWatch}\\master.jwlibrary")) File.Move($"{_folderToWatch}\\master.jwlibrary", $"{_folderToWatch}\\master.old.jwlibrary");
        

            var backupFileService = new BackupFileService(_logger);
            backupFileService.ProgressEvent += BackupFileServiceProgress;

            var backup = await backupFileService.Merge(fileNames);
            var outputFileName = $"master.jwlibrary";
            await backupFileService.WriteNewDatabase(backup, outputFileName, fileNames.First());

            var logMessage = $"{fileNames.Length} backup files merged to {outputFileName}";
            _logger?.LogInformation(logMessage);
            OnProgressEvent(logMessage);
            fileNames = Directory.EnumerateFiles(_folderToWatch, "*.jwlibrary").Where(c => !c.Contains("master.jwlibrary")).ToImmutableArray();
            foreach (var fileName in fileNames)
            {
                File.Delete(fileName);
            }
        }
        private void BackupFileServiceProgress(object? sender, ProgressEventArgs e)
        {
            OnProgressEvent(e);
        }

        private void OnProgressEvent(ProgressEventArgs e)
        {
            ProgressEvent?.Invoke(this, e);
        }

        private void OnProgressEvent(string message)
        {
            OnProgressEvent(new ProgressEventArgs { Message = message });
        }
    }
}