using System;

namespace ExamBook.Entities
{
    public class Entity
    {
        public ulong Id { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public DateTime? DeletedAt { get; set; }

        public bool IsDeleted => DeletedAt != null;
    }
}