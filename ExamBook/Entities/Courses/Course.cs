namespace ExamBook.Entities
{
    public class Course:Entity
    {
        public string Name { get; set; } = "";
        public string NormalizedName { get; set; } = "";
        public string Description { get; set; } = "";

        public Space Space { get; set; } = null!;
        public ulong SpaceId { get; set; }
    }
}