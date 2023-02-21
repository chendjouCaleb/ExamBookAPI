using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace ExamBook.Entities
{
    public class Student:Entity
    {
        public string RId { get; set; } = "";
        public string FirstName { get; set; } = "";
        public string LastName { get; set; } = "";

        public char Sex { get; set; } = 'M';
        public DateTime BirthDate { get; set; }


        [JsonIgnore]
        public User User { get; set; } = null!;
        public string? UserId { get; set; }
        
        [JsonIgnore]
        public Classroom Classroom { get; set; } = null!;

        [JsonIgnore] public HashSet<Participant> Participants { get; set; } = new();
    }
}