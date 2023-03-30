using System;
using System.Collections.Generic;

namespace Social.Entities
{
    public class Post
    {
        public long Id { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
        public DateTime? DeletedAt { get; set; }
        public string Content { get; set; } = "";

        public Author? Author { get; set; }
        public string? AuthorId { get; set; }

        /// <summary>
        /// Json post metadata.
        /// </summary>
        public string MetaData { get; set; } = "";

        // private List<Post> Children { get; set; } = new ();
        // public Post Parent { get; set; }
    }
}