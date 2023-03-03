using System.Collections.Generic;

namespace ExamBook.Entities
{
    public class CourseTeacher
    {
        public Course? Course { get; set; }
        public ulong? CourseId { get; set; }

        public Member? Member { get; set; }
        public ulong? MemberId { get; set; }

        public bool IsPrincipal { get; set; }

        public List<CourseHour> CourseHours { get; set; } = new();
        public List<CourseSession> CourseSessions { get; set; } = new();


    }
}