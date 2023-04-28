using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace ExamBook.Entities
{
    public class CourseHour:Entity
    {
        public Course Course { get; set; } = null!;
        public ulong? CourseId { get; set; }

        public DayOfWeek DayOfWeek { get; set; }
        public TimeOnly StartHour { get; set; }
        public TimeOnly EndHour { get; set; }

        public Room? Room { get; set; }
        public ulong? RoomId { get; set; }

        public Space? Space { get; set; }
        public ulong SpaceId { get; set; }
        
        [JsonIgnore]
        public CourseTeacher? CourseTeacher { get; set; }
        public ulong? CourseTeacherId { get; set; }

        [JsonIgnore]
        public List<CourseSession> CourseSessions { get; set; } = new();
    }
}