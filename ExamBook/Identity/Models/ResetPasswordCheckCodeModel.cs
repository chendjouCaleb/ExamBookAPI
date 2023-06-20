using System.ComponentModel.DataAnnotations;

namespace ExamBook.Identity.Models
{
	public class CheckCodeModel
	{
		[Required]
		public string UserId { get; set; } = "";
		
		[Required]
		public string Code { get; set; } = "";
		
		[Required]
		public string Purpose { get; set; } = "";
	}

	public class CreateCodeModel
	{
		[Required]
		public string UserId { get; set; } = "";
		
		[Required]
		[MinLength(5)]
		public string Purpose { get; set; } = "";
	}
}