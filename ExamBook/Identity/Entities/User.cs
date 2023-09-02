using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Identity;

namespace ExamBook.Identity.Entities
{
    public class User:IdentityUser
    {
        //public string Id { get; set; } = Guid.NewGuid().ToString();
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        public string FirstName { get; set; } = "";
        public string LastName { get; set; } = "";

        public DateTime BirthDate { get; set; }
        public char Sex { get; set; }


        public string AuthorId { get; set; } = "";
        public string ActorId { get; set; } = "";
        public string PublisherId { get; set; } = "";
        public string SubjectId { get; set; } = "";

        public DateTime? DeletedAt { get; set; }
        public bool Deleted { get; set; }

        public List<Session> Sessions { get; set; } = new ();
    }

    public class Role:IdentityRole
    {
        // public string Id { get; set; } = "";
        // public string Name { get; set; } = "";
        // public string NormalizedName { get; set; } = "";

        public override string ToString() => this.Name ?? "";
    }
}