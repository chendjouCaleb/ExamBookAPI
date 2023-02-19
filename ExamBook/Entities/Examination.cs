using System;
using System.Collections.Generic;

namespace ExamBook.Entities
{
    public class Examination
    {
        public Guid Id { get; set; }

        public string Name { get; set; } = "";
        public DateTime StartAt { get; set; }
        public bool IsLock { get; set; }

        public Space Space { get; set; }
        public ulong SpaceId { get; set; }

        public List<Test> Tests { get; set; } = new();
        public List<Participant> Participants { get; set; } = new List<Participant>();
    }
}