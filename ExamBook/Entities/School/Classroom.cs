using System.Collections.Generic;

namespace ExamBook.Entities
{
    public class Classroom:Entity
    {
        public string Name { get; set; } = "";

        public Member Principal { get; set; }
        
        public Space Space { get; set; }
        public ulong SpaceId { get; set; }

        public HashSet<Student> Students { get; set; } = new();
        public HashSet<Course> Courses { get; set; } = new();
        
    }
}