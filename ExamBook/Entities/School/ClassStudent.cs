namespace ExamBook.Entities
{
	public class ClassStudent
	{
		public Student Student { get; set; } = null!;
		public ulong StudentId { get; set; }

		public Classroom Classroom { get; set; } = null!;
		public ulong ClassroomId { get; set; }
	}
}