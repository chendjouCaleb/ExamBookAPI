namespace ExamBook.Models
{
	public class MemberSelectModel
	{
		public ulong? SpaceId { get; set; }
		public string? UserId { get; set; }

		public bool? IsAdmin { get; set; }
		public bool? IsTeacher { get; set; }
		public bool? IsStudent { get; set; }
	}
}