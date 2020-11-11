namespace JWLMergeCore.BackupFileServices.DataAccess
{
    using Models.DatabaseModels;
    using Microsoft.EntityFrameworkCore;

    public class BackupDbContext : DbContext
    {
        /// <inheritdoc />
#pragma warning disable 8618
        public BackupDbContext(DbContextOptions<BackupDbContext> options)
#pragma warning restore 8618
            : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.Entity<InputField>().HasNoKey();
            modelBuilder.Entity<LastModified>().HasNoKey();
        }

        public DbSet<BlockRange> BlockRanges { get; set; }
        public DbSet<Bookmark> Bookmarks { get; set; }
        public DbSet<InputField> InputFields { get; set; }
        public DbSet<LastModified> LastModified { get; set; }
        public DbSet<Location> Locations { get; set; }
        public DbSet<Note> Notes { get; set; }
        public DbSet<Tag> Tags { get; set; }
        public DbSet<TagMap> TagMaps { get; set; }
        public DbSet<UserMark> UserMarks { get; set; }
        public DbSet<PlaylistItem> PlaylistItems { get; set; }
        public DbSet<PlaylistItemChild> PlaylistItemChildren { get; set; }
        public DbSet<PlaylistMedia> PlaylistMedia { get; set; }
    }
}