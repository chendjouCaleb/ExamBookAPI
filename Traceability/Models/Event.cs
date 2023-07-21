using System;
using System.Collections.Generic;

namespace Traceability.Models
{
    public class Event
    {
        public long Id { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public string Name { get; set; } = "";
        public string DataValue { get; set; } = "";
        
        public Actor Actor { get; set; } = null!;
        public string ActorId { get; set; } = null!;

        public Subject Subject { get; set; } = null!;
        public string SubjectId { get; set; } = "";

        public List<PublisherEvent> PublisherEvents { get; set; } = new();
    }
}