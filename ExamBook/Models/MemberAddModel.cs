using ExamBook.Entities;

namespace ExamBook.Models
{
    public class MemberAddModel
    {
        public string UserId { get; set; } = null!;
        public bool IsAdmin { get; set; }
    }
}