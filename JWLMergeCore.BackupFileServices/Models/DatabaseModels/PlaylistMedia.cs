namespace JWLMergeCore.BackupFileServices.Models.DatabaseModels
{
    using System.ComponentModel.DataAnnotations.Schema;

    [Table("PlaylistMedia")]
    public class PlaylistMedia
    {
        public int PlaylistMediaId { get; set; }
        public int MediaType { get; set; }
        public string? Label { get; set; }
        public string? Filename { get; set; }
        public int LocationId { get; set; }
        public Location? Location { get; set; }
        public PlaylistMedia Clone()
        {
            return (PlaylistMedia)MemberwiseClone();
        }
    }
}