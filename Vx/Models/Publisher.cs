using System;
using System.Collections.Generic;

namespace Vx.Models
{
    public class Publisher
    {
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        public Int64 Id { get; set; }

        public HashSet<Subscriber> Subscribers { get; set; } = new ();
    }

    public class PublisherEvent
    {
        public Publisher Publisher { get; set; }
        public Event Event { get; set; }
    }
}