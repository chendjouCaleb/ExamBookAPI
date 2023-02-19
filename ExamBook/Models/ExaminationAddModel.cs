using System;

namespace ExamBook.Models
{
    public class ExaminationAddModel
    {
        public string Name { get; set; } = "";
        public DateTime StartAt { get; set; }
    }
    
    
    public class ExaminationEditModel
    {
        public string Name { get; set; } = "";
        public DateTime StartAt { get; set; }
    }
}