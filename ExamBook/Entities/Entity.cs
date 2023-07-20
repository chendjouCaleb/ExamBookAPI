using System;
using System.ComponentModel.DataAnnotations.Schema;
using Traceability.Models;

namespace ExamBook.Entities
{
    public class Entity
    {
        public ulong Id { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public string CreatedById { get; set; } = "";

        public DateTime? DeletedAt { get; set; }
        public string DeletedById { get; set; } = "";

        public bool IsDeleted => DeletedAt != null;

        public string PublisherId { get; set; } = "";
        [NotMapped] public Publisher? Publisher { get; set; }

        public string SubjectId { get; set; } = "";
        [NotMapped] public Subject Subject { get; set; } = null!;
    }
}