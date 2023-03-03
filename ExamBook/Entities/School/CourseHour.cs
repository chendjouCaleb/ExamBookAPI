using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace ExamBook.Entities
{
    public class CourseHour
    {
        public Course? Course { get; set; }

        public DayOfWeek DayOfWeek { get; set; }

        public TimeSpan StartHour { get; set; }

        public TimeSpan EndHour { get; set; }

        public ulong ClassroomId { get; set; }
        
        [JsonIgnore]
        public CourseTeacher? CourseTeacher { get; set; }
        public long? CourseTeacherId { get; set; }

        
        public List<CourseSession> CourseSessions { get; set; } = new();
    }
}