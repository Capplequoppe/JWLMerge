namespace JWLMergeCore.BackupFileServices.Models.DatabaseModels
{
    using System.ComponentModel.DataAnnotations.Schema;

    [Table("InputField")]
    public class InputField
    {
        public int LocationId { get; set; }

        public string? TextTag { get; set; }

        public string? Value { get; set; }

        public InputField Clone()
        {
            return (InputField)MemberwiseClone();
        }
    }
}