using System;

namespace ExamBook.Entities
{
    public class Member:Entity
    {
        public string UserId { get; set; } = "";
        
        public Space? Space { get; set; }
        public ulong SpaceId { get; set; }
        
        public bool IsAdmin { get; set; }

        public bool IsTeacher { get; set; }
        
        
    }
}