namespace ExamBook.Models
{
	public class ChangeNameModel
	{
		public ChangeNameModel() {}
		public ChangeNameModel(string firstName, string lastName)
		{
			FirstName = firstName;
			LastName = lastName;
		}

		public string FirstName { get; set; } = "";
		public string LastName { get; set; } = "";
	}
}