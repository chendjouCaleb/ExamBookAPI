using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using Vx.Models;

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

        public string PublisherId { get; set; } = null!;
        [NotMapped] public Publisher? Publisher { get; set; } 

        /// <summary>
        /// Json post metadata.
        /// </summary>
        public string MetaData { get; set; } = "";

        public bool IsResponse => ParentPost != null;

        public Post? ParentPost { get; set; }
        public long? ParentPostId { get; set; }

        public List<PostFile> Files { get; set; } = new();

        // private List<Post> Children { get; set; } = new ();
        // public Post Parent { get; set; }
    }
}