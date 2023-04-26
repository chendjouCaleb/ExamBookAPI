using Newtonsoft.Json;

namespace ExamBook.Entities
{
    public class CourseSpeciality:Entity
    {
        
        [JsonIgnore]
        public Course? Course { get; set; }
        public ulong? CourseId { get; set; }


        [JsonIgnore]
        public Speciality? Speciality { get; set; }
        public ulong SpecialityId { get; set; }
    }
}