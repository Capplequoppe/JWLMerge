namespace JWLMergeCore.BackupFileServices.Events
{
    using System;

    public class ProgressEventArgs : EventArgs
    {
        public ProgressEventArgs()
        {
            Message = string.Empty;
        }
        public string Message { get; set; }
    }
}