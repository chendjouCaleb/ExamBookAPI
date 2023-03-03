using System;

namespace ExamBook.Identity.Models
{
    public class User
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public DateTime CreatedAt { get; set; }
        public string UserName { get; set; } = "";
        public string NormalizedUserName { get; set; } = "";

        public string Email { get; set; } = "";
        public string NormalizedEmail { get; set; } = "";
        
        public string FirstName { get; set; } = "";
        public string LastName { get; set; } = "";

        public DateTime BirthDate { get; set; }
        public char Sex { get; set; }

        public DateTime? DeletedAt { get; set; }
        public bool Deleted { get; set; }
    }

    public class Role
    {
        public string Id { get; set; } = "";
        public string Name { get; set; } = "";
        public string NormalizedName { get; set; } = "";

        public override string ToString() => this.Name ?? "";
    }
}