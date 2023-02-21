using System;
using Microsoft.AspNetCore.Identity;

namespace ExamBook.Entities
{
    public class User:IdentityUser<string>
    {
        public DateTime CreatedAt { get; set; }
        public string FirstName { get; set; } = "";
        public string LastName { get; set; } = "";

        public DateTime BirthDate { get; set; }
        public char Sex { get; set; }
    }
}