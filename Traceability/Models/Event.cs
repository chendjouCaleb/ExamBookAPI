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

        public List<PublisherEvent> PublisherEvents { get; set; } = new();
        public List<SubjectEvent> SubjectEvents { get; set; } = new();
        public List<ActorEvent> ActorEvents { get; set; } = new ();
    }
}