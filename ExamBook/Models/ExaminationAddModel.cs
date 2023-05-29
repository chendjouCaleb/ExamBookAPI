using System;
using System.Collections.Generic;

namespace ExamBook.Models
{
    public class ExaminationAddModel
    {
        public string Name { get; set; } = "";
        public DateTime StartAt { get; set; }

        public HashSet<ulong> SpecialitiyIds { get; set; }
    }
}