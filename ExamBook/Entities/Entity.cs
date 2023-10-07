using System;
using System.ComponentModel.DataAnnotations.Schema;
using Traceability.Models;

namespace ExamBook.Entities
{
    public class Entity
    {
        public ulong Id { get; set; }
        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
        
        public DateTimeOffset? DeletedAt { get; set; }

        public bool IsDeleted => DeletedAt != null;

        public string PublisherId { get; set; } = "";
        [NotMapped] public Publisher? Publisher { get; set; }

        public string SubjectId { get; set; } = "";
        [NotMapped] public Subject Subject { get; set; } = null!;
    }
}