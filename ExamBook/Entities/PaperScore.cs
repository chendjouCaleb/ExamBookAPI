namespace ExamBook.Entities
{
	public class PaperScore
	{
		public ulong Id { get; set; }
		public float? Value { get; set; }

		public Paper Paper { get; set; } = null!;
		public ulong PaperId { get; set; }
	}
}