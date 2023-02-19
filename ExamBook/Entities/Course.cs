namespace ExamBook.Entities
{
    public class Course:Entity
    {
        public string Name { get; set; } = "";
        public string Description { get; set; } = "";

        public string Code { get; set; } = "";

        public uint Coefficient { get; set; }
    }
}