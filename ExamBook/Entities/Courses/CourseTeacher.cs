using System.Collections.Generic;
using Newtonsoft.Json;

namespace ExamBook.Entities
{
    public class CourseTeacher:Entity
    {
	    public CourseClassroom CourseClassroom { get; set; } = null!;
        public ulong? CourseClassroomId { get; set; }

        public Member? Member { get; set; }
        public ulong? MemberId { get; set; }

        public bool IsPrincipal { get; set; } = true;

       [JsonIgnore] public List<CourseHour> CourseHours { get; set; } = new();
       [JsonIgnore] public List<CourseSession> CourseSessions { get; set; } = new();


    }
}