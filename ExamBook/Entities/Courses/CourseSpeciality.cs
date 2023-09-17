namespace ExamBook.Entities
{
    public class CourseSpeciality:Entity
    {


        public CourseClassroom CourseClassroom { get; set; } = null!;
        public ulong? CourseClassroomId { get; set; }


        public Speciality? Speciality { get; set; }
        public ulong SpecialityId { get; set; }
    }
}