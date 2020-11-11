namespace JWLMergeCore.BackupFileServices.Models.ManifestFile
{
    using System;

    /// <summary>
    /// The manifest file.
    /// </summary>
    /// <remarks>We implement INotifyPropertyChanged to prevent the common "WPF binding leak".</remarks>
    public sealed class Manifest 
    {
        public Manifest()
        {
            Name = string.Empty;
            CreationDate = DateTime.Now.ToString("YYYY-MM-dd");
            UserDataBackup = new UserDataBackup();
        }

        /// <summary>
        /// The name of the backup file (without the "jwlibrary" extension).
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// The local creation date in the form "YYYY-MM-DD"
        /// </summary>
        public string CreationDate { get; set; }

        /// <summary>
        /// The manifest schema version.
        /// </summary>
        public int Version { get; set; }

        /// <summary>
        /// The type. Semantics unknown!
        /// </summary>
        public int Type { get; set; }

        /// <summary>
        /// Details of the backup database.
        /// </summary>
        public UserDataBackup UserDataBackup { get; set; }

        public Manifest Clone()
        {
            return (Manifest)MemberwiseClone();
        }
    }
}