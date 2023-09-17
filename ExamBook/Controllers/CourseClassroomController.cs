using System;
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
	
	[Route("api/courses")]
	public class CourseController:ControllerBase
	{
		private readonly CourseService _courseService;
		private readonly UserService _userService;
		private readonly SpaceService _spaceService;
		private readonly MemberService _memberService;
		private readonly ApplicationDbContext _dbContext;

		public CourseController(CourseService courseService,
			UserService userService, SpaceService spaceService, 
			ApplicationDbContext dbContext, MemberService memberService)
		{
			_courseService = courseService;
			_userService = userService;
			_spaceService = spaceService;
			_dbContext = dbContext;
			_memberService = memberService;
		}


		[HttpGet("{courseId}")]
		public async Task<Course> GetAsync(ulong courseId)
		{
			var course = await _courseService.GetAsync(courseId);

			course.CourseSpecialities = await _dbContext.CourseSpecialities
				.Include(cs => cs.Speciality)
				.Where(cs => cs.CourseId == courseId)
				.ToListAsync();
			
			course.CourseTeachers = await _dbContext.CourseTeachers
				.Include(ct => ct.Member)
				.Where(cs => cs.CourseId == courseId)
				.ToListAsync();
			var memberUserId = course.CourseTeachers.Select(ct => ct.Member!.UserId!).ToList();
			var users = await _userService.ListById(memberUserId);
			foreach (var courseTeacher in course.CourseTeachers)
			{
				courseTeacher.Member!.User = users.Find(u => u.Id == courseTeacher.Member.UserId);
			}
			return course;
		}

		
		[HttpGet]
		public async Task<ICollection<Course>> ListAsync([FromQuery] ulong? spaceId)
		{
			IQueryable<Course> query = _dbContext.Courses
				.Include(c => c.Space);

			if (spaceId != null)
			{
				query = query.Where(c =>c.SpaceId == spaceId);
			}

			var courses = await query.ToListAsync();

			// var courseTeachers = courses.SelectMany(c => c.CourseTeachers).ToList();
			//
			// var memberUserId = courseTeachers.Select(ct => ct.Member!.UserId!).ToList();
			// var users = await _userService.ListById(memberUserId);
			// foreach (var courseTeacher in courseTeachers)
			// {
			// 	courseTeacher.Member!.User = users.Find(u => u.Id == courseTeacher.Member.UserId);
			// }

			return courses;
		}


		[HttpGet("contains")]
		public async Task<bool> ContainsAsync([FromQuery] ulong spaceId, [FromQuery] string name, [FromQuery] string code)
		{

			if (!string.IsNullOrWhiteSpace(name))
			{
				var normalizedName = StringHelper.Normalize(name);
				return await _dbContext.Courses
					.Where(c => c.SpaceId == spaceId && c.NormalizedName == normalizedName)
					.AnyAsync();
			}
			
			if (!string.IsNullOrWhiteSpace(code))
			{
				var normalizedCode = StringHelper.Normalize(code);
				return await _dbContext.Courses
					.Where(c => c.SpaceId == spaceId && c.NormalizedCode == normalizedCode)
					.AnyAsync();
			}

			return false;
		}
		
		

		[Authorize]
		[HttpPost]
		public async Task<CreatedAtActionResult> AddAsync([FromQuery] ulong spaceId,
			[FromBody] CourseAddModel model)
		{
			var userId = HttpContext.User.FindFirst(ClaimTypes.NameIdentifier)!.Value;
			var user = await _userService.GetByIdAsync(userId);
			var space = await _spaceService.GetByIdAsync(spaceId);
			var member = await _memberService.AuthorizeAsync(space, userId);
			
			var course = (await _courseService.AddCourseAsync(space, model, member)).Item;
			course = await _courseService.GetAsync(course.Id);
			
			return CreatedAtAction("Get", new {courseId = course.Id}, course);
		}


		[Authorize]
		[HttpPut("{courseId}/name")]
		public async Task<OkObjectResult> ChangeNameAsync(ulong courseId, [FromBody] IDictionary<string, string> body)
		{
			var userId = HttpContext.User.FindFirst(ClaimTypes.NameIdentifier)!.Value;
			var user = await _userService.GetByIdAsync(userId);
			var course = await _courseService.GetAsync(courseId);
			var member = await _memberService.AuthorizeAsync(course.Space, userId);
			string name = body["name"];
			var result = await _courseService.ChangeCourseNameAsync(course, name, user);
			return Ok(result);
		}
		
		
		[Authorize]
		[HttpPut("{courseId}/code")]
		public async Task<OkObjectResult> ChangeCodeAsync(ulong courseId, [FromBody] IDictionary<string, string> body)
		{
			var userId = HttpContext.User.FindFirst(ClaimTypes.NameIdentifier)!.Value;
			var user = await _userService.GetByIdAsync(userId);
			var course = await _courseService.GetAsync(courseId);
            
			string code = body["code"];
			var result = await _courseService.ChangeCourseCodeAsync(course, code, user);
			return Ok(result);
		}
		
		
			
		[Authorize]
		[HttpPut("{courseId}/coefficient")]
		public async Task<OkObjectResult> ChangeCoefficientAsync(ulong courseId, [FromBody] IDictionary<string, string> body)
		{
			var userId = HttpContext.User.FindFirst(ClaimTypes.NameIdentifier)!.Value;
			var user = await _userService.GetByIdAsync(userId);
			var course = await _courseService.GetAsync(courseId);
            
			string value = body["coefficient"];
			var coefficient = uint.Parse(value);
			var result = await _courseService.ChangeCourseCoefficientAsync(course, coefficient, user);
			return Ok(result);
		}
		

		[Authorize]
		[HttpPut("{courseId}/description")]
		public async Task<OkObjectResult> ChangeDescriptionAsync(ulong courseId, [FromBody] IDictionary<string, string> body)
		{
			var userId = HttpContext.User.FindFirst(ClaimTypes.NameIdentifier)!.Value;
			var user = await _userService.GetByIdAsync(userId);
			var course = await _courseService.GetAsync(courseId);
            
			string description = body["description"];
			var member = await _memberService.AuthorizeAsync(course.Space!, userId);
			var result = await _courseService.ChangeCourseDescriptionAsync(course, description, member);
			return Ok(result);
		}
		
		
		[Authorize]
		[HttpDelete("{courseId}")]
		public async Task<OkObjectResult> DeleteAsync(ulong courseId)
		{
			var userId = HttpContext.User.FindFirst(ClaimTypes.NameIdentifier)!.Value;
			var user = await _userService.GetByIdAsync(userId);
			var course = await _courseService.GetAsync(courseId);

			var member = await _memberService.AuthorizeAsync(course.Space!, userId);
			var result = await _courseService.DeleteAsync(course, member);
			return Ok(result);
		}
		
	}
}