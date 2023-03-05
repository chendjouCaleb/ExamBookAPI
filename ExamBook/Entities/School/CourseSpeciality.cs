namespace ExamBook.Entities
{
    public class CourseSpeciality:Entity
    {
        public Course? Course { get; set; }
        public ulong? CourseId { get; set; }


        public ClassroomSpeciality? ClassroomSpeciality { get; set; }
        public ulong ClassroomSpecialityId { get; set; }
    }
}