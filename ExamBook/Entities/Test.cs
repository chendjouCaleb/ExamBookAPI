using System;

namespace ExamBook.Entities
{
    public class Test
    {
        public Examination Examination { get; set; }
        
        public string Name { get; set; } = "";
        public DateTime StartAt { get; set; }
        public uint Coefficient { get; set; }
    }
}