using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using ExamBook.Entities;
using ExamBook.Helpers;
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
		private readonly SpaceService _spaceService;
		private readonly MemberService _memberService;
		private readonly ApplicationDbContext _dbContext;

		public CourseController(CourseService courseService,
			SpaceService spaceService, 
			ApplicationDbContext dbContext, MemberService memberService)
		{
			_courseService = courseService;
			_spaceService = spaceService;
			_dbContext = dbContext;
			_memberService = memberService;
		}


		[HttpGet("{courseId}")]
		public async Task<Course> GetAsync(ulong courseId)
		{
			var course = await _courseService.GetAsync(courseId);
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

			return courses;
		}


		[HttpGet("contains")]
		public async Task<bool> ContainsAsync([FromQuery] ulong spaceId, [FromQuery] string name)
		{

			if (!string.IsNullOrWhiteSpace(name))
			{
				var normalizedName = StringHelper.Normalize(name);
				return await _dbContext.Courses
					.Where(c => c.SpaceId == spaceId && c.NormalizedName == normalizedName)
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
			var course = await _courseService.GetAsync(courseId);
			var member = await _memberService.AuthorizeAsync(course.Space, userId);
			string name = body["name"];
			var result = await _courseService.ChangeCourseNameAsync(course, name, member);
			return Ok(result);
		}
		
		
		

		[Authorize]
		[HttpPut("{courseId}/description")]
		public async Task<OkObjectResult> ChangeDescriptionAsync(ulong courseId, [FromBody] IDictionary<string, string> body)
		{
			var userId = HttpContext.User.FindFirst(ClaimTypes.NameIdentifier)!.Value;
			var course = await _courseService.GetAsync(courseId);
            
			string description = body["description"];
			var member = await _memberService.AuthorizeAsync(course.Space, userId);
			var result = await _courseService.ChangeCourseDescriptionAsync(course, description, member);
			return Ok(result);
		}
		
		
		[Authorize]
		[HttpDelete("{courseId}")]
		public async Task<OkObjectResult> DeleteAsync(ulong courseId)
		{
			var userId = HttpContext.User.FindFirst(ClaimTypes.NameIdentifier)!.Value;
			var course = await _courseService.GetAsync(courseId);

			var member = await _memberService.AuthorizeAsync(course.Space, userId);
			var result = await _courseService.DeleteAsync(course, member);
			return Ok(result);
		}
		
	}
}