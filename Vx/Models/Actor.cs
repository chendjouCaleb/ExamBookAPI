using System;
using System.Collections.Generic;

namespace Vx.Models
{
    /// <summary>
    /// Bridge between your user and app user. 
    /// </summary>
    public class Actor
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        

        public List<Event> Events { get; set; } = new();
    }
}