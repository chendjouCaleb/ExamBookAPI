using System;

namespace ExamBook.Entities
{
    public class Entity
    {
        public ulong Id { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public string CreatedById { get; set; } = "";

        public DateTime? DeletedAt { get; set; }
        public string DeletedById { get; set; } = "";

        public bool IsDeleted => DeletedAt != null;
    }
}