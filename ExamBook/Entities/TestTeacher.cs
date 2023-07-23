namespace ExamBook.Entities
{
	public class TestTeacher:Entity
	{
		public Member Member { get; set; }
		public ulong? MemberId { get; set; }

		public Test Test { get; set; }
		public ulong? TestId { get; set; }
	}
}