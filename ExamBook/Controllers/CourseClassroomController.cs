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
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ExamBook.Controllers
{
	[Route("api/course-classrooms")]
	public class CourseClassroomController : ControllerBase
	{
		private readonly CourseService _courseService;
		private readonly CourseClassroomService _courseClassroomService;
		private readonly ClassroomService _classroomService;
		private readonly UserService _userService;
		private readonly MemberService _memberService;
		private readonly ApplicationDbContext _dbContext;

		public CourseClassroomController(CourseService courseService,
			UserService userService,
			ApplicationDbContext dbContext, MemberService memberService, CourseClassroomService courseClassroomService,
			ClassroomService classroomService)
		{
			_courseService = courseService;
			_userService = userService;
			_dbContext = dbContext;
			_memberService = memberService;
			_courseClassroomService = courseClassroomService;
			_classroomService = classroomService;
		}


		[HttpGet("{courseClassroomId}")]
		public async Task<CourseClassroom> GetAsync(ulong courseClassroomId)
		{
			var courseClassroom = await _courseClassroomService.GetAsync(courseClassroomId);

			courseClassroom.CourseSpecialities = await _dbContext.CourseSpecialities
				.Include(cs => cs.ClassroomSpeciality.Speciality)
				.Where(cs => cs.CourseClassroomId == courseClassroomId)
				.ToListAsync();

			courseClassroom.CourseTeachers = await _dbContext.CourseTeachers
				.Include(ct => ct.Member)
				.Where(cs => cs.CourseClassroomId == courseClassroomId)
				.ToListAsync();
			var memberUserId = courseClassroom.CourseTeachers.Select(ct => ct.Member!.UserId!).ToList();
			var users = await _userService.ListById(memberUserId);
			foreach (var courseTeacher in courseClassroom.CourseTeachers)
			{
				courseTeacher.Member!.User = users.Find(u => u.Id == courseTeacher.Member.UserId);
			}

			return courseClassroom;
		}


		[HttpGet]
		public async Task<ICollection<CourseClassroom>> ListAsync(
			[FromQuery] ulong? spaceId,
			[FromQuery] ulong? courseId
		)
		{
			IQueryable<CourseClassroom> query = _dbContext.CourseClassrooms
				.Include(c => c.Course.Space);

			if (spaceId != null)
			{
				query = query.Where(c => c.Course.SpaceId == spaceId);
			}

			if (courseId != null)
			{
				query = query.Where(c => c.CourseId == courseId);
			}

			var courseClassrooms = await query.ToListAsync();

			var courseClassroomTeachers = courseClassrooms.SelectMany(c => c.CourseTeachers).ToList();

			var memberUserId = courseClassroomTeachers.Select(ct => ct.Member!.UserId!).ToList();
			var users = await _userService.ListById(memberUserId);
			foreach (var courseClassroomTeacher in courseClassroomTeachers)
			{
				courseClassroomTeacher.Member!.User = users.Find(u => u.Id == courseClassroomTeacher.Member.UserId);
			}

			return courseClassrooms;
		}


		[HttpGet("contains")]
		public async Task<bool> ContainsAsync([FromQuery] ulong classroomId, [FromQuery] ulong? courseId,
			[FromQuery] string code)
		{
			if (courseId != null)
			{
				return await _dbContext.CourseClassrooms
					.Where(c => c.ClassroomId == classroomId && c.CourseId == courseId)
					.AnyAsync();
			}

			if (!string.IsNullOrWhiteSpace(code))
			{
				var normalizedCode = StringHelper.Normalize(code);
				return await _dbContext.CourseClassrooms
					.Where(c => c.ClassroomId == classroomId && c.NormalizedCode == normalizedCode)
					.AnyAsync();
			}

			return false;
		}


		[Authorize]
		[HttpPost]
		public async Task<CreatedAtActionResult> AddAsync([FromQuery] ulong classroomId,
			[FromQuery] ulong courseId,
			[FromBody] CourseClassroomAddModel model)
		{
			var userId = HttpContext.User.FindFirst(ClaimTypes.NameIdentifier)!.Value;
			var classroom = await _classroomService.GetByIdAsync(classroomId);
			var course = await _courseService.GetAsync(courseId);

			var member = await _memberService.AuthorizeAsync(classroom.Space, userId);

			var courseClassroom = (await _courseClassroomService.AddAsync(classroom, course, model, member)).Item;

			return CreatedAtAction("Get", new {courseClassroomId = courseClassroom.Id}, courseClassroom);
		}


		[Authorize]
		[HttpPut("{courseClassroomId}/code")]
		public async Task<OkObjectResult> ChangeCodeAsync(ulong courseClassroomId,
			[FromBody] IDictionary<string, string> body)
		{
			var userId = HttpContext.User.FindFirst(ClaimTypes.NameIdentifier)!.Value;
			var courseClassroom = await _courseClassroomService.GetAsync(courseClassroomId);
			var member = await _memberService.AuthorizeAsync(courseClassroom.Classroom.Space, userId);

			string code = body["code"];
			var result = await _courseClassroomService.ChangeCodeAsync(courseClassroom, code, member);
			return Ok(result);
		}


		[Authorize]
		[HttpPut("{courseClassroomId}/coefficient")]
		public async Task<OkObjectResult> ChangeCoefficientAsync(ulong courseClassroomId,
			[FromBody] IDictionary<string, string> body)
		{
			var userId = HttpContext.User.FindFirst(ClaimTypes.NameIdentifier)!.Value;
			var courseClassroom = await _courseClassroomService.GetAsync(courseClassroomId);
			var member = await _memberService.AuthorizeAsync(courseClassroom.Classroom.Space, userId);

			string value = body["coefficient"];
			var coefficient = uint.Parse(value);
			var result = await _courseClassroomService.ChangeCoefficientAsync(courseClassroom, coefficient, member);
			return Ok(result);
		}


		[Authorize]
		[HttpPut("{courseClassroomId}/description")]
		public async Task<OkObjectResult> ChangeDescriptionAsync(ulong courseClassroomId,
			[FromBody] IDictionary<string, string> body)
		{
			var userId = HttpContext.User.FindFirst(ClaimTypes.NameIdentifier)!.Value;
			var courseClassroom = await _courseClassroomService.GetAsync(courseClassroomId);
			var member = await _memberService.AuthorizeAsync(courseClassroom.Classroom.Space, userId);

			string description = body["description"];
			var result = await _courseClassroomService.ChangeDescriptionAsync(courseClassroom, description, member);
			return Ok(result);
		}


		[Authorize]
		[HttpDelete("{courseClassroomId}")]
		public async Task<OkObjectResult> DeleteAsync(ulong courseClassroomId)
		{
			var userId = HttpContext.User.FindFirst(ClaimTypes.NameIdentifier)!.Value;
			var courseClassroom = await _courseClassroomService.GetAsync(courseClassroomId);

			var member = await _memberService.AuthorizeAsync(courseClassroom.Classroom.Space, userId);
			var result = await _courseClassroomService.DeleteAsync(courseClassroom, member);
			return Ok(result);
		}
	}
}