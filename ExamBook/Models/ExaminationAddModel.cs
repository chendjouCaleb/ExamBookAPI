using System;
using System.Collections.Generic;

namespace ExamBook.Models
{
    public class ExaminationAddModel
    {
        public string Name { get; set; } = "";
        public DateTime StartAt { get; set; }
    }


    public class ExaminationSpecialityAddModel
    {
        public string Name { get; set; } = "";
        public string Description { get; set; } = "";
    }
}