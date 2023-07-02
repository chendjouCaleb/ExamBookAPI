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
	
	[Route("api/courseSpecialities")]
	public class CourseSpecialityController:ControllerBase
	{
		
		private readonly CourseService _courseService;
		private readonly SpecialityService _specialityService;
		private readonly UserService _userService;
		private readonly ApplicationDbContext _dbContext;

		public CourseSpecialityController(SpecialityService specialityService, 
			UserService userService, 
			ApplicationDbContext dbContext, 
			CourseService courseService)
		{
			_specialityService = specialityService;
			_userService = userService;
			_dbContext = dbContext;
			_courseService = courseService;
		}


		[HttpGet("{courseSpecialityId}")]
		public async Task<CourseSpeciality> GetAsync(ulong courseSpecialityId)
		{
			var courseSpeciality = await _dbContext.CourseSpecialities
				.Include(cs => cs.Course)
				.Include(cs => cs.Speciality)
				.Where(cs => cs.Id == courseSpecialityId)
				.FirstOrDefaultAsync();

			if (courseSpeciality == null)
			{
				throw new ElementNotFoundException("CourseSpecialityNotFoundById", courseSpecialityId);
			}

			return courseSpeciality;
		}

		
		[HttpGet]
		public async Task<IList<CourseSpeciality>> ListAsync([FromQuery] ulong? courseId, [FromQuery] ulong? specialityId)
		{
			IQueryable<CourseSpeciality> query = _dbContext.CourseSpecialities
				.Include(cs => cs.Course)
				.Include(cs => cs.Speciality);

			if (courseId != null)
			{
				query = query.Where(cs => cs.CourseId == courseId);
			}
			
			if (specialityId != null)
			{
				query = query.Where(cs => cs.SpecialityId == specialityId);
			}

			return await query.ToListAsync();
		}
		
		
		[Authorize]
		[HttpPost]
		public async Task<OkObjectResult> AddSpecialityAsync(ulong courseId, [FromBody] ulong specialityId)
		{
			var userId = HttpContext.User.FindFirst(ClaimTypes.NameIdentifier)!.Value;
			var user = await _userService.GetByIdAsync(userId);
			var course = await _courseService.GetCourseAsync(courseId);
			var speciality = await _specialityService.GetAsync(specialityId);


			var result = await _courseService.AddCourseSpecialityAsync(course, speciality, user);
			return Ok(result.Item);
		}
		
		[Authorize]
		[HttpPost("{courseId}/specialities")]
		public async Task<OkObjectResult> AddSpecialitiesAsync(ulong courseId, [FromBody] HashSet<ulong> specialityId)
		{
			var userId = HttpContext.User.FindFirst(ClaimTypes.NameIdentifier)!.Value;
			var user = await _userService.GetByIdAsync(userId);
			var course = await _courseService.GetCourseAsync(courseId);
			var specialities = specialityId
				.Select(async id => await _specialityService.GetAsync(id))
				.Select(t => t.Result)
				.ToList();


			var result = await _courseService.AddCourseSpecialitiesAsync(course, specialities, user);
			return Ok(result.Item);
		}


		[Authorize]
		[HttpDelete("{courseSpecialityId}")]
		public async Task<OkObjectResult> DeleteAsync(ulong courseSpecialityId)
		{
			var userId = HttpContext.User.FindFirst(ClaimTypes.NameIdentifier)!.Value;
			var user = await _userService.GetByIdAsync(userId);
			
			var courseSpeciality = await _courseService.GetCourseSpecialityAsync(courseSpecialityId);
			var result = await _courseService.DeleteCourseSpecialityAsync(courseSpeciality, user);
			return Ok(result);
		}
	}
}