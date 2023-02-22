using System.Collections.Generic;

namespace ExamBook.Models
{
    public class ClassroomAddModel
    {
        public string Name { get; set; } = "";

        public List<ulong> SpecialityIds { get; set; } = new();
    }
}