using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace ExamBook.Entities
{
    public class CourseHour:Entity
    {
        public Course? Course { get; set; }

        public DayOfWeek DayOfWeek { get; set; }

        public TimeSpan StartHour { get; set; }

        public TimeSpan EndHour { get; set; }

        public Room? Room { get; set; }
        public ulong? RoomId { get; set; }

        public Classroom? Classroom { get; set; }
        public ulong ClassroomId { get; set; }
        
        [JsonIgnore]
        public CourseTeacher? CourseTeacher { get; set; }
        public ulong? CourseTeacherId { get; set; }

        
        public List<CourseSession> CourseSessions { get; set; } = new();
    }
}