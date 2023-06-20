using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace ExamBook.Models
{
    public class ApplyAddModel
    {
        
        [Required]
        public string Code { get; set; } = "";

        [Required]
        public string FirstName { get; set; } = "";
        
        [Required]
        public string LastName { get; set; } = "";
        
        [Required]
        public DateTime BirthDate { get; set; }
        
        [Required]
        public char Sex { get; set; }

        public string UserId { get; set; } = "";

        public HashSet<ulong> SpecialityIds { get; set; } = new();
    }

    public class ApplyAcceptModel
    {
        public string Code { get; set; } = "";
    }
    
    
    public class ApplyChangeInfoModel
    {
        [Required]
        public string FirstName { get; set; } = "";
        
        [Required]
        public string LastName { get; set; } = "";
        
        [Required]
        public DateTime BirthDate { get; set; }
        
        [Required]
        public char Sex { get; set; }
        
        [Required]
        public string Code { get; set; } = "";
        
        
    }
}