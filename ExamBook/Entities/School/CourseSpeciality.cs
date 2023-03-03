using ExamBook.Entities;

namespace ExamBook.Entities
{
    public class CourseSpeciality
    {
        public Course? Course { get; set; }
        public ulong? CourseId { get; set; }


        public ClassroomSpeciality? ClassroomSpeciality { get; set; }
        public ulong ClassroomSpecialityId { get; set; }
    }
}