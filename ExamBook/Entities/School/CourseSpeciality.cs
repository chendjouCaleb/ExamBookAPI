using Newtonsoft.Json;

namespace ExamBook.Entities
{
    public class CourseSpeciality:Entity
    {
        
     
        public Course? Course { get; set; }
        public ulong? CourseId { get; set; }


        public Speciality? Speciality { get; set; }
        public ulong SpecialityId { get; set; }
    }
}