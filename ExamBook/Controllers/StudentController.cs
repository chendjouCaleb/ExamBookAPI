using System.Threading.Tasks;
using ExamBook.Entities;
using ExamBook.Identity.Services;
using ExamBook.Persistence;
using ExamBook.Services;
using Microsoft.AspNetCore.Mvc;

namespace ExamBook.Controllers
{
	
	[Route("api/students")]
	public class StudentController:ControllerBase
	{
		private readonly StudentService _studentService;
		private readonly SpaceService _spaceService;
		private readonly UserService _userService;
		private readonly ApplicationDbContext _dbContext;

		public StudentController(StudentService studentService, SpaceService spaceService, UserService userService, ApplicationDbContext dbContext)
		{
			_studentService = studentService;
			_spaceService = spaceService;
			_userService = userService;
			_dbContext = dbContext;
		}


		[HttpGet("{studentId}")]
		public async Task<Student> GetAsync(ulong studentId)
		{
			return await _studentService.
		}
	}
}