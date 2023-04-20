using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using Vx.Models;

namespace Social.Entities
{
    public class Author
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Name { get; set; } = "";
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? DeletedAt { get; set; }

        public string PictureId { get; set; } = "";
        public string ThumbnailId { get; set; } = "";
        public string ActorId { get; set; } = null!;
        
        [NotMapped]
        public Actor? Actor { get; set; }
        
        public string PublisherId { get; set; } = null!;
        
        [NotMapped]
        public Publisher? Publisher { get; set; }

        public HashSet<AuthorSubscription> Subscriptions { get; set; } = new();
    }
}