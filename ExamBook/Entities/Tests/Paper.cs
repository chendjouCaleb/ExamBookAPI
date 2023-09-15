using System.ComponentModel.DataAnnotations.Schema;

namespace ExamBook.Entities
{
    public class Paper:Entity
    {
        public uint IndexInGroup { get; set; }
        
        [NotMapped] public bool IsCorrected => PaperScore?.Value != null;

        public bool IsPresent { get; set; }


        public Participant? Participant { get; set; }
        public ulong? ParticipantId { get; set; }

        public Student? Student { get; set; }
        public ulong? StudentId { get; set; }

        public Test Test { get; set; } = null!;
        public ulong TestId { get; set; }

        public TestSpeciality? TestSpeciality { get; set; }
        public ulong? TestSpecialityId { get; set; }

        public PaperScore? PaperScore { get; set; }
    }
}