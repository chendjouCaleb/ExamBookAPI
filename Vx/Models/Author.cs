using System;
using System.Collections.Generic;

namespace Vx.Models
{
    /// <summary>
    /// Bridge between your user and app user. 
    /// </summary>
    public class Author
    {
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public UInt64 Id { get; set; }

        public List<Event> Events { get; set; } = new();
    }
}