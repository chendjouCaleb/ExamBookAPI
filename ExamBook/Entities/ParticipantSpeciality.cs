namespace ExamBook.Entities
{
    public class ParticipantSpeciality:Entity
    {
        public Participant Participant { get; set; } = null!;
        public ulong ParticipantId { get; set; }

        public ExaminationSpeciality ExaminationSpeciality { get; set; } = null!;
        public ulong ExaminationSpecialityId { get; set; }
    }
}