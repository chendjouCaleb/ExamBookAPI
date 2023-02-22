using System;
using System.Collections.Generic;
using ExamBook.Entities.School;
using Newtonsoft.Json;

namespace ExamBook.Entities
{
    public class Examination:Entity
    {
        public string Name { get; set; } = "";
        public DateTime StartAt { get; set; }
        public bool IsLock { get; set; }

        [JsonIgnore]
        public Space Space { get; set; } = null!;
        public ulong SpaceId { get; set; }


        [JsonIgnore]
        public Classroom? Classroom { get; set; }

        public ulong? ClassroomId { get; set; }
        

        [JsonIgnore] public List<Test> Tests { get; set; } = new();
        [JsonIgnore] public List<Participant> Participants { get; set; } = new ();
    }
}