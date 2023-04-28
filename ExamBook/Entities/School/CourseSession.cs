using System;
using Newtonsoft.Json;

namespace ExamBook.Entities
{
    public class CourseSession:Entity
    {
        public Space Space{ get; set; }
        public ulong SpaceId { get; set; }
        
        public CourseHour? CourseHour { get; set; }
        public ulong? CourseHourId { get; set; }

        public Course? Course { get; set; }
        public ulong? CourseId { get; set; }

        public Room? Room { get; set; }
        public ulong? RoomId { get; set; }
        
        [JsonIgnore]
        public CourseTeacher? CourseTeacher { get; set; }
        public ulong? CourseTeacherId { get; set; }
        
        public DateTime? ExpectedStartDateTime { get; set; }
        public DateTime? ExpectedEndDateTime { get; set; }
        
        public DateTime? StartDateTime { get; set; }
        public DateTime? EndDateTime { get; set; }

        public string Description { get; set; } = "";
        public string Report { get; set; } = "";
    }

    
}