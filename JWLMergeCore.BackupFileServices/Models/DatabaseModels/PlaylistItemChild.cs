namespace JWLMergeCore.BackupFileServices.Models.DatabaseModels
{
    using System.ComponentModel.DataAnnotations.Schema;

    [Table("PlaylistItemChild")]
    public class PlaylistItemChild
    {
        public int PlaylistItemChildId { get; set; }
        public int BaseDurationTicks { get; set; }
        public int MarkerId { get; set; }
        public string? MarkerLabel { get; set; }
        public int MarkerStartTimeTicks { get; set; }
        public int MarkerEndTransitionDurationTicks { get; set; }
        public PlaylistItem? PlaylistItem { get; set; }
        public int PlaylistItemId { get; set; }
        public PlaylistItemChild Clone()
        {
            return (PlaylistItemChild)MemberwiseClone();
        }
    }
}