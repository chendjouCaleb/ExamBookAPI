﻿using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace ExamBook.Models
{
    public class StudentAddModel
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

    public class StudentChangeRIdModel
    {
        public string RId { get; set; } = "";
    }
    
    public class StudentChangeInfoModel
    {
        [Required]
        public string FirstName { get; set; } = "";
        
        [Required]
        public string LastName { get; set; } = "";
        
        [Required]
        public DateTime BirthDate { get; set; }
        
        [Required]
        public char Sex { get; set; }
    }
}