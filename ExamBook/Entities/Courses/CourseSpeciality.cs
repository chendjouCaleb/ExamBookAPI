using Newtonsoft.Json;

namespace ExamBook.Entities
{
    public class CourseSpeciality:Entity
    {
        
     
        public CourseClassroom? CourseClassroom { get; set; }
        public ulong? CourseClassroomId { get; set; }


        public Speciality? Speciality { get; set; }
        public ulong SpecialityId { get; set; }
    }
}