namespace ExamBook.Entities
{
    public class Speciality:Entity
    {
        public Space? Space { get; set; }
        public ulong? SpaceId { get; set; }
        
        
        public string Name { get; set; } = "";
        public string NormalizedName { get; set; } = "";
    }
}