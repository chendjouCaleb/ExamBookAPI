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
	
	[Route("api/courseTeachers")]
	public class CourseTeacherController:ControllerBase
	{
		private readonly CourseService _courseService;
		private readonly CourseClassroomService _courseClassroomService;
		private readonly CourseTeacherService _courseTeacherService;
		private readonly MemberService _memberService;
		private readonly UserService _userService;
		private readonly ApplicationDbContext _dbContext;

		public CourseTeacherController(
			UserService userService, 
			ApplicationDbContext dbContext, 
			CourseService courseService, MemberService memberService, CourseTeacherService courseTeacherService, CourseClassroomService courseClassroomService)
		{
			_userService = userService;
			_dbContext = dbContext;
			_courseService = courseService;
			_memberService = memberService;
			_courseTeacherService = courseTeacherService;
			_courseClassroomService = courseClassroomService;
		}


		[HttpGet("{courseTeacherId}")]
		public async Task<CourseTeacher> GetAsync(ulong courseTeacherId)
		{
			var courseTeacher = await _dbContext.CourseTeachers
				.Include(cs => cs.CourseClassroom.Course)
				.Include(cs => cs.Member)
				.Where(cs => cs.Id == courseTeacherId)
				.FirstOrDefaultAsync();

			if (courseTeacher == null)
			{
				throw new ElementNotFoundException("CourseTeacherNotFoundById", courseTeacherId);
			}

			return courseTeacher;
		}

		
		[HttpGet]
		public async Task<IList<CourseTeacher>> ListAsync(
			[FromQuery] ulong? courseId, 
			[FromQuery] ulong? courseClassroomId,
			[FromQuery] ulong? memberId)
		{
			IQueryable<CourseTeacher> query = _dbContext.CourseTeachers
				.Include(cs => cs.CourseClassroom.Course)
				.Include(cs => cs.Member);

			if (courseId != null)
			{
				query = query.Where(cs => cs.CourseClassroom.CourseId == courseId);
			}
			
			if (courseId != null)
			{
				query = query.Where(cs => cs.CourseClassroomId == courseClassroomId);
			}
			
			if (memberId != null)
			{
				query = query.Where(cs => cs.MemberId == memberId);
			}

			return await query.ToListAsync();
		}
		
		
		
		
		[Authorize]
		[HttpPost]
		public async Task<OkObjectResult> AddTeachersAsync(ulong courseClassroomId, [FromBody] HashSet<ulong> memberId)
		{
			var userId = HttpContext.User.FindFirst(ClaimTypes.NameIdentifier)!.Value;
			var user = await _userService.GetByIdAsync(userId);
			var courseClassroom = await _courseClassroomService.GetAsync(courseClassroomId);
			var member = await _memberService.GetAsync(courseClassroom.Course.Space, userId);
			var members = memberId
				.Select(async id => await _memberService.GetByIdAsync(id))
				.Select(t => t.Result)
				.ToList();

			var result = await _courseTeacherService.AddCourseTeachersAsync(courseClassroom, members, member);
			return Ok(result.Item);
		}


		[Authorize]
		[HttpDelete("{courseTeacherId}")]
		public async Task<OkObjectResult> DeleteAsync(ulong courseTeacherId)
		{
			var userId = HttpContext.User.FindFirst(ClaimTypes.NameIdentifier)!.Value;
			var user = await _userService.GetByIdAsync(userId);
			
			var courseTeacher = await _courseTeacherService.GetAsync(courseTeacherId);
			var member = await _memberService.AuthorizeAsync(courseTeacher.Member!.Space, userId);
			var result = await _courseTeacherService.DeleteAsync(courseTeacher, member);
			return Ok(result);
		}
	}
}