using System.Collections.Generic;
using Newtonsoft.Json;

namespace ExamBook.Entities
{
    public class CourseTeacher:Entity
    {
        public Course? Course { get; set; }
        public ulong? CourseId { get; set; }

        public Member? Member { get; set; }
        public ulong? MemberId { get; set; }

        public bool IsPrincipal { get; set; }

       [JsonIgnore] public List<CourseHour> CourseHours { get; set; } = new();
       [JsonIgnore] public List<CourseSession> CourseSessions { get; set; } = new();


    }
}