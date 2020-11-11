namespace JWLMergeCore.BackupFileServices.Models.DatabaseModels
{
    using System.ComponentModel.DataAnnotations.Schema;
    using Newtonsoft.Json;

    [Table("lastModified")]
    public class LastModified
    {
        /// <summary>
        /// Time stamp when the database was last modified.
        /// </summary>
        [JsonProperty(PropertyName = "LastModified")]
        [Column(name:"LastModified")]
        public string? TimeLastModified { get; set; }

        public void Reset()
        {
            TimeLastModified = null;
        }
    }
}