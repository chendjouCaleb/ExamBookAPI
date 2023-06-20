using System.ComponentModel.DataAnnotations;

namespace ExamBook.Identity.Models
{
	public class ChangePasswordModel
	{
		[Required] 
		[MinLength(6)] 
		public string CurrentPassword { get; set; } = "";

		[Required] 
		[MinLength(6)] 
		public string NewPassword { get; set; } = "";
	}
}