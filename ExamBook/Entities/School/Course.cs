using System.Collections.Generic;
using Newtonsoft.Json;

namespace ExamBook.Entities
{
    public class Course:Entity
    {
        public string Name { get; set; } = "";
        public string NormalizedName { get; set; } = "";
        public string Description { get; set; } = "";

        public string Code { get; set; } = "";
        public string NormalizedCode { get; set; } = "";

        public uint Coefficient { get; set; }

        /// <summary>
        /// Tells if the course is restricted to some specialities.
        /// If the course has speciality this value should be false.
        /// If the course hasn't speciality, this value should be true.
        /// </summary>
        public bool IsGeneral { get; set; }

        public Space? Space { get; set; }
        public ulong SpaceId { get; set; }

         public List<CourseSpeciality> CourseSpecialities { get; set; } = new();
         public List<CourseSession> CourseSessions { get; set; } = new();
         public List<CourseHour> CourseHours { get; set; } = new();
         public List<CourseTeacher> CourseTeachers { get; set; } = new();
    }
}