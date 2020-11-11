namespace JWLMergeCore.BackupFileServices.Models
{
    using ManifestFile;
    using DatabaseModels;

    /// <summary>
    /// The Backup file.
    /// </summary>
    /// <remarks>We implement INotifyPropertyChanged to prevent the common "WPF binding leak".</remarks>
    public sealed class BackupFile
    {
        public BackupFile()
        {
            Manifest = new Manifest();
            Database = new Database();
        }
        public Manifest Manifest { get; set; }
        
        public Database Database { get; set; }
    }
}