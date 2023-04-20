using System;

namespace Social.Entities
{
    public class Repost
    {
        public long Id { get; set; }
        public DateTime CreatedAt { get; set; }
        
        public Author? Author { get; set; }
        public string? AuthorId { get; set; }

        public Post? Post { get; set; }
        public long? PostId { get; set; }

        public Post? ChildPost { get; set; }
        public long? ChildPostId { get; set; }
    }
}