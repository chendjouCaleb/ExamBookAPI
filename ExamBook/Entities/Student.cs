namespace ExamBook.Entities
{
    public class Student:Entity
    {
        public string FirstName { get; set; } = "";
        public string LastName { get; set; } = "";


        public string? UserId { get; set; }
        public Classroom Classroom { get; set; } = null!;
    }
}