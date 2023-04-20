using System;
using System.Collections.Generic;

namespace Vx.Models
{
    public class Event
    {
        public long Id { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public string Name { get; set; } = "";
        public string DataValue { get; set; } = "";
        
        public Actor Actor { get; set; } = null!;
        public string ActorId { get; set; } = null!;

        public List<PublisherEvent> PublisherEvents { get; set; } = new();
    }
}