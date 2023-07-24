using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using ExamBook.Entities;
using ExamBook.Exceptions;
using ExamBook.Identity.Services;
using ExamBook.Persistence;
using ExamBook.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ExamBook.Controllers
{
	[Route("api/test-teachers")]
	public class TestTeacherController:ControllerBase
	{
		private readonly ApplicationDbContext _dbContext;
		private readonly UserService _userService;
		private readonly TestTeacherService _testTeacherService;
		private readonly TestService _testService;
		private readonly MemberService _memberService;


		public TestTeacherController(ApplicationDbContext dbContext, 
			UserService userService, TestTeacherService testTeacherService, 
			TestService testService, MemberService memberService)
		{
			_dbContext = dbContext;
			_userService = userService;
			_testTeacherService = testTeacherService;
			_testService = testService;
			_memberService = memberService;
		}

		[HttpGet("{testTeacherId}")]
		[Authorize]
		public async Task<TestTeacher> GetAsync(ulong testTeacherId)
		{
			var testTeacher = await _dbContext.TestTeachers
				.Include(tt => tt.Test)
				.Include(tt => tt.Member)
				.Where(tt => tt.Id == testTeacherId)
				.FirstOrDefaultAsync();

			if (testTeacher == null)
			{
				throw new ElementNotFoundException("TestTeacherNotFoundById", testTeacherId);
			}

			return testTeacher;
		}


		[HttpGet]
		[Authorize]
		public async Task<ICollection<TestTeacher>> ListAsync([FromQuery] ulong? testId, [FromQuery] ulong? memberId)
		{
			var query = _dbContext.TestTeachers
				.Include(tt => tt.Test)
				.Include(tt => tt.Member)
				.AsQueryable();

			if (testId != null)
			{
				query = query.Where(tt => tt.TestId == testId);
			}
			
			if (memberId != null)
			{
				query = query.Where(tt => tt.MemberId == memberId);
			}

			return await query.ToListAsync();
		}


		[HttpPost]
		[Authorize]
		public async Task<CreatedAtActionResult> AddAsync([FromQuery] ulong testId, [FromQuery] ulong memberId)
		{
			var userId = HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier)!;
			var user = await _userService.FindByIdAsync(userId);
			
			var member = await _memberService.GetByIdAsync(memberId);
			var test = await _testService.GetByIdAsync(testId);

			var result = await _testTeacherService.AddAsync(test, member, user);
			var testTeacher = result.Item;

			return CreatedAtAction("Get", new {TestTeacher = testTeacher.Id}, testTeacher);
		}


		[HttpDelete("{testTeacherId}")]
		[Authorize]
		public async Task<NoContentResult> DeleteAsync(ulong testTeacherId)
		{
			var userId = HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier)!;
			var user = await _userService.FindByIdAsync(userId);

			var testTeacher = await _testTeacherService.GetAsync(testTeacherId);
			await _testTeacherService.DeleteAsync(testTeacher, user);

			return NoContent();
		}
	}
}