using System;

namespace Vx.Models
{
    public class Event
    {
        public UInt64 Id { get; set; }
        public Author? Author { get; set; }
        public UInt64 AuthorId { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public string Name { get; set; } = "";
        public string DataValue { get; set; } = "";
    }
}