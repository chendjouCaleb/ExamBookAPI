using System;
using ExamBook.Identity.Models;

namespace ExamBook.Identity.Entities
{
    public class Session
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? EndAt { get; set; }

        public bool IsClose => EndAt != null;


        public User User { get; set; } = null!;
        public string UserId { get; set; } = default!;
    }
}