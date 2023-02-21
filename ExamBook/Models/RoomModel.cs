using System.ComponentModel.DataAnnotations;

namespace ExamBook.Models
{
    public class RoomAddModel
    {
        [Required]
        public string Name { get; set; } = "";
        public uint Capacity { get; set; }
    }

    public class RoomChangeNameModel
    {
        [Required]
        public string Name { get; set; } = "";
    }
    
    public class RoomChangeCapacityModel
    {
        [Required]
        public uint Capacity { get; set; } 
    }
    
    public class RoomChangeInfoModel
    {
        public uint Capacity { get; set; }
    }
}