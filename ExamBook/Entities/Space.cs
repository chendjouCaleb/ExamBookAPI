using System.Collections.Generic;

namespace ExamBook.Entities
{
    public class Space:Entity
    {
        public string Name { get; set; } = "";
        public string Identifier { get; set; } = "";
        
        public HashSet<Member> Members { get; set; } = new();
    }
}