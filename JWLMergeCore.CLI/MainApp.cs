namespace JWLMergeCore.CLI
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using BackupFileServices;
    using BackupFileServices.Events;
    using Exceptions;
    using System.Threading.Tasks;

    /// <summary>
    /// The main app.
    /// </summary>
    internal sealed class MainApp
    {
        public MainApp()
        {
            ProgressEvent += MainApp_ProgressEvent;
        }

        private static void MainApp_ProgressEvent(object? sender, ProgressEventArgs e)
        {
        }

        public event EventHandler<ProgressEventArgs> ProgressEvent;
        
        /// <summary>
        /// Runs the app.
        /// </summary>
        /// <param name="args">Program arguments</param>
        public async Task Run(string[] args, string? fileName = null)
        {
            var files = GetInputFiles(args);
            
            IBackupFileService backupFileService = new BackupFileService();
            backupFileService.ProgressEvent += BackupFileServiceProgress;
            
            var backup = await backupFileService.Merge(files);
            var outputFileName = $"{fileName ?? backup.Manifest.Name}.jwlibrary";
            await backupFileService.WriteNewDatabase(backup, outputFileName, files.First());

            var logMessage = $"{files.Count} backup files merged to {outputFileName}";
            //Log.Logger.Information(logMessage);
            OnProgressEvent(logMessage);
        }

        private void BackupFileServiceProgress(object? sender, ProgressEventArgs? e)
        {
            OnProgressEvent(e);
        }

        private IReadOnlyCollection<string> GetInputFiles(string[] args)
        {
            OnProgressEvent("Checking files exist");
            
            var result = new List<string>();
            
            foreach (var arg in args)
            {
                if (!File.Exists(arg))
                {
                    throw new JwlMergeCmdLineException($"File does not exist: {arg}");
                }
                
                //Log.Logger.Debug("Found file: {file}", arg);
                result.Add(arg);
            }

            if (result.Count < 2)
            {
                throw new JwlMergeCmdLineException("Specify at least 2 files to merge");
            }

            return result;
        }

        private void OnProgressEvent(ProgressEventArgs? e)
        {
            ProgressEvent?.Invoke(this, e ?? new ProgressEventArgs());
        }

        private void OnProgressEvent(string? message)
        {
            OnProgressEvent(new ProgressEventArgs { Message = message ?? "" });
        }
    }
}