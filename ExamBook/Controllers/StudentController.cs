using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using ExamBook.Entities;
using ExamBook.Helpers;
using ExamBook.Identity.Services;
using ExamBook.Models;
using ExamBook.Persistence;
using ExamBook.Services;
using ExamBook.Utils;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ExamBook.Controllers
{
	
	[Route("api/students")]
	public class StudentController:ControllerBase
	{
		private readonly StudentService _studentService;
		private readonly MemberService _memberService;
		private readonly SpaceService _spaceService;
		private readonly UserService _userService;
		private readonly ApplicationDbContext _dbContext;

		public StudentController(StudentService studentService, 
			SpaceService spaceService, 
			UserService userService, 
			ApplicationDbContext dbContext, 
			MemberService memberService)
		{
			_studentService = studentService;
			_spaceService = spaceService;
			_userService = userService;
			_dbContext = dbContext;
			_memberService = memberService;
		}


		[HttpGet("{studentId}")]
		public async Task<Student> GetAsync(ulong studentId)
		{
			var student = await _studentService.GetByIdAsync(studentId);

			return student;
		}
		
		
		[HttpGet("contains")]
		public async Task<bool> ContainsAsync([FromQuery] ulong spaceId, [FromQuery] string code)
		{

			if (!string.IsNullOrWhiteSpace(code))
			{
				var normalizedCode = StringHelper.Normalize(code);
				return await _dbContext.Students
					.Where(c => c.SpaceId == spaceId && c.NormalizedCode == normalizedCode)
					.AnyAsync();
			}
			
			return false;
		}

		[HttpGet]
		public async Task<ICollection<Student>> ListAsync(
			[FromQuery] ulong? spaceId, [FromQuery] ulong? memberId, [FromQuery] string userId)
		{
			IQueryable<Student> query = _dbContext.Students
				.Include(s => s.Member);

			if (spaceId != null)
			{
				query = query.Where(s => s.SpaceId == spaceId)
					.Include(s => s.Member);
			}

			if (memberId != null)
			{
				query = query.Where(m => m.MemberId == memberId);
			}

			if (!string.IsNullOrWhiteSpace(userId))
			{
				query = query.Where(m => m.Member != null && m.Member.UserId == userId)
					.Include(s => s.Space);
			}

			var students = await query.ToListAsync();

			var members = students
				.Where(s => s.Member != null)
				.Select(ct => ct.Member!)
				.ToList();
			
			var memberUserId = members.Select(m => m.UserId!).ToList();

			var users = await _userService.ListById(memberUserId);
			foreach (var member in members)
			{
				member.User = users.Find(u => u.Id == member.UserId);
			}

			return students;
		}


		[Authorize]
		[HttpPost]
		public async Task<CreatedAtActionResult> AddAsync([FromQuery] ulong spaceId, [FromForm] StudentAddModel model)
		{
			AssertHelper.NotNull(model, nameof(model));
			
			var userId = HttpContext.User.FindFirst(ClaimTypes.NameIdentifier)!.Value;
			var user = await _userService.GetByIdAsync(userId);
			var space = await _spaceService.GetByIdAsync(spaceId);

			var result = await _studentService.AddAsync(space, model, user);
			await _dbContext.Entry(result.Item).ReloadAsync();
			return CreatedAtAction("Get", new {studentId = result.Item.Id}, result.Item);
		}
		
		
		[Authorize]
		[HttpPut("{studentId}/attach")]
		public async Task<OkObjectResult> AttachAsync(ulong studentId, [FromQuery] string userId)
		{
			var userAuthorId = HttpContext.User.FindFirst(ClaimTypes.NameIdentifier)!.Value;
			var userAuthor = await _userService.GetByIdAsync(userAuthorId);
			
			var student = await _studentService.GetByIdAsync(studentId);
			var member = await _memberService.GetOrAddAsync(student.Space, userId, userAuthor);

			var result = await _studentService.AttachAsync(student, member, userAuthor);
			return Ok(result);
		}
		
		
		
		[Authorize]
		[HttpPut("{studentId}/detach")]
		public async Task<OkObjectResult> DetachAsync(ulong studentId)
		{
			var userAuthorId = HttpContext.User.FindFirst(ClaimTypes.NameIdentifier)!.Value;
			var userAuthor = await _userService.GetByIdAsync(userAuthorId);
			
			var student = await _studentService.GetByIdAsync(studentId);

			var result = await _studentService.DetachAsync(student, userAuthor);
			return Ok(result);
		}
		
		
		[Authorize]
		[HttpPut("{studentId}/code")]
		public async Task<OkObjectResult> ChangeCodeAsync(ulong studentId, [FromBody] IDictionary<string, string> body)
		{
			var userId = HttpContext.User.FindFirst(ClaimTypes.NameIdentifier)!.Value;
			var user = await _userService.GetByIdAsync(userId);
			var student = await _studentService.GetByIdAsync(studentId);
            
			string code = body["code"];
			var result = await _studentService.ChangeCodeAsync(student, code, user);
			return Ok(result);
		}
		
		
		[Authorize]
		[HttpPut("{studentId}/info")]
		public async Task<OkObjectResult> ChangeInfoAsync(ulong studentId, [FromBody] StudentChangeInfoModel model)
		{
			AssertHelper.NotNull(model, nameof(model));
			var userId = HttpContext.User.FindFirst(ClaimTypes.NameIdentifier)!.Value;
			var user = await _userService.GetByIdAsync(userId);
			var student = await _studentService.GetByIdAsync(studentId);
            
			var result = await _studentService.ChangeInfoAsync(student, model, user);
			return Ok(result);
		}


		[Authorize]
		[HttpDelete("{studentId}")]
		public async Task<OkObjectResult> DeleteAsync(ulong studentId)
		{
			var userId = HttpContext.User.FindFirst(ClaimTypes.NameIdentifier)!.Value;
			var user = await _userService.GetByIdAsync(userId);
			var student = await _studentService.GetByIdAsync(studentId);

			var result = await _studentService.DeleteAsync(student, user);
			return Ok(result);
		}
	}
}