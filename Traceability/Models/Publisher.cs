using System;

namespace Traceability.Models
{
    
    /// <summary>
    /// The publisher is an object which can emit event.
    /// For instance: When a social post is created, a publisher is also created for it.
    /// Then, when the post is edited or deleted, publisher can emit an event.
    ///
    /// The publisher will serve to identify all events emitted for the post,
    /// without coupling post and emitted events.
    /// </summary>
    
    public class Publisher
    {
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Name { get; set; } = "";
    }

    public class PublisherEvent
    {
        public PublisherEvent(Publisher publisher, Event @event)
        {
            Publisher = publisher;
            Event = @event;
        }
        
        public long Id { get; set; }
        public Publisher? Publisher { get; set; }
        public string PublisherId { get; set; } = null!;
        
        public Event? Event { get; set; } 
        public long? EventId { get; set; }
    }
}