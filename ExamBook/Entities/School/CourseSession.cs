using System;
using Newtonsoft.Json;

namespace ExamBook.Entities
{
    public class CourseSession
    {
        public CourseHour? CourseHour { get; set; }
        public ulong? CourseHourId { get; set; }

        public Course? Course { get; set; }
        public ulong? CourseId { get; set; }

        public Room? Room { get; set; }
        public ulong? RoomId { get; set; }
        
        [JsonIgnore]
        public CourseTeacher? CourseTeacher { get; set; }
        public long? CourseTeacherId { get; set; }
        
        public DateTime? ExpectedStartDate { get; set; }
        public DateTime? ExpectedEndDate { get; set; }
        
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }

        public string Objective { get; set; } = "";
        public string Report { get; set; } = "";
        public bool Lecture { get; set; }
    }
}