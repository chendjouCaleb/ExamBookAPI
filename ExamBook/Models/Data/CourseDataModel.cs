using ExamBook.Entities;

namespace ExamBook.Models.Data
{
	public class CourseDataModel
	{
		public CourseDataModel(Course course)
		{
			Name = course.Name;
			Description = course.Description;
		}

		public CourseDataModel()
		{ }
		
		public string Name { get; set; } = "";
		public string Description { get; set; } = "";
	}
}