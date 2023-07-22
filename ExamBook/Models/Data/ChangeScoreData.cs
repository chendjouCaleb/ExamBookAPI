namespace ExamBook.Models.Data
{
	public class ChangeScoreData
	{
		public ulong PaperId { get; set; }
		public float? Last { get; set; }
		public float? Current { get; set; }
	}
}