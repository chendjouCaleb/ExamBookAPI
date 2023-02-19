namespace ExamBook.Entities
{
    public class Member
    {
        public Space Space { get; set; } = null!;

        public string UserId { get; set; } = null!;
        public ulong SpaceId { get; set; }
        
        public bool IsAdmin { get; set; }

        public bool IsTeacher { get; set; }
        
        
    }
}