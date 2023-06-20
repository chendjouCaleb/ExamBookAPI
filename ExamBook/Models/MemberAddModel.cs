namespace ExamBook.Models
{
    public class MemberAddModel
    {
        public string UserId { get; set; } = null!;
        public bool IsAdmin { get; set; }

        public bool IsTeacher { get; set; }
    }
}