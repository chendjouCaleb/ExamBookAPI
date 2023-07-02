using System.Collections.Generic;
using Newtonsoft.Json;

namespace ExamBook.Entities
{
    public class Speciality:Entity
    {
        public Space? Space { get; set; }
        public ulong? SpaceId { get; set; }
        public string Name { get; set; } = "";
        public string NormalizedName { get; set; } = "";

        public string Description { get; set; } = "";

        [JsonIgnore] public List<CourseSpeciality> CourseSpecialities { get; set; } = new ();
        [JsonIgnore] public List<StudentSpeciality> StudentSpecialities { get; set; } = new ();
        [JsonIgnore] public List<ExaminationSpeciality> ExaminationSpecialities { get; set; } = new ();
    }
}