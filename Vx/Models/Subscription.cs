using System;

namespace Vx.Models
{
    
    /// <summary>
    /// 
    /// </summary>
    public class Subscription
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public Publisher? Publisher { get; set; }
        public string? PublisherId { get; set; }
        
    }
}