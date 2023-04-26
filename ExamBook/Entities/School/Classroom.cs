using System.Collections.Generic;
using Newtonsoft.Json;

namespace ExamBook.Entities
{
    public class Classroom:Entity
    {
        public string Name { get; set; } = "";
        public string NormalizedName { get; set; } = "";
        public Space? Space { get; set; }
        public ulong SpaceId { get; set; }

        public Room? Room { get; set; }
        public ulong? RoomId { get; set; }

        [JsonIgnore] public HashSet<Student> Students { get; set; } = new();
        //[JsonIgnore] public HashSet<Course> Courses { get; set; } = new();
        [JsonIgnore] public List<ClassroomSpeciality> ClassroomSpecialities { get; set; } = new();
    }


    public class ClassroomSpeciality : Entity
    {
        public ClassroomSpeciality()
        { }

        public ClassroomSpeciality(Classroom? classroom, Speciality? speciality)
        {
            Classroom = classroom;
            Speciality = speciality;
        }
        
        

        public Classroom? Classroom { get; set; }
        public ulong? ClassroomId { get; set; }

        public Speciality? Speciality { get; set; }
        public ulong SpecialityId { get; set; }

        
    }
}