using System;

namespace Traceability.Models
{
    /// <summary>
    /// Bridge between your user and app user. 
    /// </summary>
    public class Actor
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Name { get; set; } = "";
        
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}