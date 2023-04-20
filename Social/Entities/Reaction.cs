using System;
using Social.Helpers;

namespace Social.Entities
{
    public class Reaction
    {
        public long Id { get; set; }
        public DateTime CreateAt { get; set; } = DateTime.UtcNow;
        
        public string Type { get; set; } = StringHelper.Normalize("like");

        public Post? Post { get; set; }
        public long PostId { get; set; }

        public Author? Author { get; set; }
        public string AuthorId { get; set; } = "";
        
    }
}