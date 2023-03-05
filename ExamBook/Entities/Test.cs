using System;

namespace ExamBook.Entities
{
    public class Test:Entity
    {
        public Examination Examination { get; set; } = null!;
        public ulong ExaminationId { get; set; }

        public Room? Room { get; set; } = null!;
        public ulong? RoomId { get; set; }
        
        public string Name { get; set; } = "";
        public string NormalizedName { get; set; } = "";
        
        public DateTime StartAt { get; set; }

        /// <summary>
        /// Duration in minutes.
        /// </summary>
        public uint Duration { get; set; } = 60;
        public uint Coefficient { get; set; }
        public uint Radical { get; set; }

        public bool Closed { get; set; }
        public bool Specialized { get; set; }
    }
}