using System;

namespace Vx.Models
{
    public class Subscriber
    {
        public Int64 Id { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public Publisher? Publisher { get; set; }
        
    }
}