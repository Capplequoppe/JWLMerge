namespace JWLMergeCore.BackupFileServices.Models
{
    internal class TagTypeAndName
    {
        public TagTypeAndName(int type, string name)
        {
            TagType = type;
            Name = name;
        }

        public int TagType { get; }

        public string Name { get; }

        public override int GetHashCode() => new { TagType, Name }.GetHashCode();

        public override bool Equals(object? obj) =>
            obj switch
            {
                null => false,
                TagTypeAndName o => TagType == o.TagType && Name == o.Name,
                _ => false
            };
    }
}