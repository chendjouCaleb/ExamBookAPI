using System.ComponentModel.DataAnnotations;
using System.IO;

namespace ExamBook.Models
{
    public class SpaceAddModel
    {
        
        [Required]
        public string Name { get; set; } = "";

        [Required]
        public string Identifier { get; set; } = "";

        public string Description { get; set; } = "";
        
        /// <summary>
        /// Tells if anyone can see public information of this space.
        /// If the space is private, only her members and students or admin can see it.
        /// </summary>
        public bool IsPublic { get; set; }
        
        public string Twitter { get; set; } = "";
        public string Youtube { get; set; } = "";
        public string Facebook { get; set; } = "";
        public string Instagram { get; set; } = "";
        public string Website { get; set; } = "";
        
        
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