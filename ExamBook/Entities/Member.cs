using System;
using System.ComponentModel.DataAnnotations.Schema;
using ExamBook.Identity.Entities;
using Newtonsoft.Json;

namespace ExamBook.Entities
{
    public class Member:Entity
    {
        public string? UserId { get; set; }
        [NotMapped] public User? User { get; set; } = null!;
        
        [JsonIgnore]
        public Space? Space { get; set; }
        public ulong SpaceId { get; set; }
        
        
        public bool IsAdmin { get; set; }
        public bool IsTeacher { get; set; }
    }
}