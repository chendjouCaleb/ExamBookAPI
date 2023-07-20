namespace ExamBook.Entities
{
	public class ExaminationStudent:Entity
	{
		public Student Student { get; set; } = null!;
		public ulong StudentId { get; set; }

		public Participant? Participant { get; set; } = null!;
		public ulong? ParticipantId { get; set; }
	}
}