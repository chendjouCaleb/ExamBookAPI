using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace ExamBook.Models
{
    public class ParticipantAddModel
    {
        
        [Required]
        public string RId { get; set; } = "";

        [Required]
        public string FirstName { get; set; } = "";
        
        [Required]
        public string LastName { get; set; } = "";
        
        [Required]
        public DateOnly BirthDate { get; set; }
        
        [Required]
        public char Sex { get; set; }

        public HashSet<ulong> ExaminationSpecialityIds { get; set; } = new();
    }

    public class ParticipantChangeRIdModel
    {
        public string RId { get; set; } = "";
    }
    
    public class ParticipantChangeInfoModel
    {
        [Required]
        public string FirstName { get; set; } = "";
        
        [Required]
        public string LastName { get; set; } = "";
        
        [Required]
        public DateOnly BirthDate { get; set; }
        
        [Required]
        public char Sex { get; set; }
    }
}