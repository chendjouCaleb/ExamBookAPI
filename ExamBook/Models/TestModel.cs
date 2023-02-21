using System;
using System.Collections.Generic;

namespace ExamBook.Models
{
    public class TestAddModel
    {
        public string Name { get; set; } = "";
        public DateTime StartAt { get; set; }
        
        public uint Duration { get; set; } = 60;
        
        public uint Coefficient { get; set; }

        public uint Radical { get; set; }

        public ulong RoomId { get; set; }
        
        public HashSet<ulong> ExaminationSpecialityIds { get; set; } = new();
        
    }
}