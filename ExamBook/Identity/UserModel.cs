using System;
using System.ComponentModel.DataAnnotations;

namespace ExamBook.Identity
{
    public class UserAddModel
    {
        [Required] public string FirstName { get; set; } = "";

        [Required] public string LastName { get; set; } = "";

        [Required] public char Sex { get; set; } = 'M';
        
        [Required]
        public DateTime? BirthDate { get; set; }


        [Required] public string UserName { get; set; } = "";
        [Required] public string Password { get; set; } = "";
    }
}