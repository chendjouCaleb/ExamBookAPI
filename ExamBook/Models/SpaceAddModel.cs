using System.ComponentModel.DataAnnotations;

namespace ExamBook.Models
{
    public class SpaceAddModel
    {
        
        [Required]
        public string Name { get; set; } = "";

        [Required]
        public string Identifier { get; set; } = "";
    }

    public class ChangeSpaceIdentifierModel
    {
        public string Identifier { get; set; } = "";
    }

    public class SpaceChangeInfoModel
    {
        [Required]
        public string Name { get; set; } = "";
    }
}