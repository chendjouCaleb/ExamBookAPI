using System;

namespace ExamBook.Identity.Entities
{
    public class UserCode
    {
        public ulong Id { get; set; }
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdateDate { get; set; }

        
        /// <summary>
        /// Identifier for user(Id, phoneNumber, email, etc...).
        /// </summary>
        public string UserId { get; set; } = "";

        public string NormalizedUserId { get; set; } = "";

        public string CodeHash { get; set; } = "";

        public string Purpose { get; set; } = "";
    }

    public class UserCodeCreateResult
    {
        public UserCode UserCode { get; set; } = null!;
        public string Code { get; set; } = null!;
    }
}