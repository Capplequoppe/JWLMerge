namespace JWLMergeCore.BackupFileServices.Models.DatabaseModels
{
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations.Schema;

    [Table("PlaylistItem")]
    public class PlaylistItem
    {
        public PlaylistItem()
        {
            Children = new List<PlaylistItemChild>();
        }
        public int PlaylistItemId { get; set; }
        public string? Label { get; set; }
        public int AccuracyStatement { get; set; }
        public int StartTimeOffsetTicks { get; set; }
        public int EndTimeOffsetTicks { get; set; }
        public int EndAction { get; set; }
        public string? ThumbnailFilename { get; set; }
        public PlaylistMedia? PlaylistMedia { get; set; }
        public int PlaylistMediaId { get; set; }
        public ICollection<PlaylistItemChild> Children { get; set; }
        public PlaylistItem Clone()
        {
            return (PlaylistItem)MemberwiseClone();
        }
    }
}