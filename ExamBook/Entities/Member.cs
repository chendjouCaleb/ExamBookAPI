using System;
using Newtonsoft.Json;

namespace ExamBook.Entities
{
    public class Member:Entity
    {
        public string? UserId { get; set; }
        
        [JsonIgnore]
        public Space? Space { get; set; }
        public ulong SpaceId { get; set; }
        
        
        public bool IsAdmin { get; set; }
        public bool IsTeacher { get; set; }
    }
}