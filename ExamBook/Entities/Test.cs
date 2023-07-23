using System;
using System.Collections.Generic;

namespace ExamBook.Entities
{
    public class Test:Entity
    {
        public string Name { get; set; } = "";
        public string NormalizedName { get; set; } = "";
        
        public string Code { get; set; } = "";
        public string NormalizedCode { get; set; } = "";
        
        public DateTime StartAt { get; set; }

        /// <summary>
        /// Duration in minutes.
        /// </summary>
        public uint Duration { get; set; } = 60;
        public uint Coefficient { get; set; }
        public uint Radical { get; set; }
        
        public Space Space { get; set; } = null!;
        public ulong SpaceId { get; set; }
        
        public Examination? Examination { get; set; }
        public ulong? ExaminationId { get; set; }
        
        public Course? Course { get; set; } 
        public ulong? CourseId { get; set; }
        

        public bool Closed { get; set; }
        public bool IsLock { get; set; }

        public bool IsPublished { get; set; }
        public bool Specialized { get; set; }
        
        public List<TestSpeciality> TestSpecialities { get; set; }
        public List<TestTeacher> TestTeachers { get; set; } = new();
    }
}