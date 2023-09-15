using System.Collections.Generic;

namespace ExamBook.Entities
{
    public class Course:Entity
    {
        public string Name { get; set; } = "";
        public string NormalizedName { get; set; } = "";
        public string Description { get; set; } = "";

        public Space? Space { get; set; }
        public ulong SpaceId { get; set; }
    }
}