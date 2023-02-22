using System.Collections.Generic;

namespace ExamBook.Entities.School
{
    public class Classroom:Entity
    {
        public string Name { get; set; } = "";
        public string NormalizedName { get; set; } = "";
        public Space? Space { get; set; }
        public ulong SpaceId { get; set; }

        public HashSet<Student> Students { get; set; } = new();
        public HashSet<Course> Courses { get; set; } = new();
        
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