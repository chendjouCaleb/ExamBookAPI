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
	public class CourseSpecialityController : ControllerBase
	{
		private readonly MemberService _memberService;
		private readonly CourseClassroomService _courseClassroomService;
		private readonly CourseSpecialityService _courseSpecialityService;
		private readonly ClassroomSpecialityService _classroomSpecialityService;
		private readonly UserService _userService;
		private readonly ApplicationDbContext _dbContext;

		public CourseSpecialityController(
			UserService userService,
			ApplicationDbContext dbContext,
			CourseClassroomService courseClassroomService,
			ClassroomSpecialityService classroomSpecialityService,
			CourseSpecialityService courseSpecialityService, MemberService memberService)
		{
			_userService = userService;
			_dbContext = dbContext;
			_courseClassroomService = courseClassroomService;
			_classroomSpecialityService = classroomSpecialityService;
			_courseSpecialityService = courseSpecialityService;
			_memberService = memberService;
		}


		[HttpGet("{courseSpecialityId}")]
		public async Task<CourseSpeciality> GetAsync(ulong courseSpecialityId)
		{
			var courseSpeciality = await _dbContext.CourseSpecialities
				.Include(cs => cs.CourseClassroom.Course.Space)
				.Include(cs => cs.ClassroomSpeciality.Speciality)
				.Where(cs => cs.Id == courseSpecialityId)
				.FirstOrDefaultAsync();

			if (courseSpeciality == null)
			{
				throw new ElementNotFoundException("CourseSpecialityNotFoundById", courseSpecialityId);
			}

			return courseSpeciality;
		}


		[HttpGet]
		public async Task<IList<CourseSpeciality>> ListAsync([FromQuery] ulong? courseId,
			[FromQuery] ulong? courseClassroomId,
			[FromQuery] ulong? classroomSpecialityId,
			[FromQuery] ulong? specialityId)
		{
			IQueryable<CourseSpeciality> query = _dbContext.CourseSpecialities
				.Include(cs => cs.CourseClassroom)
				.Include(cs => cs.ClassroomSpeciality);

			if (courseId != null)
			{
				query = query.Where(cs => cs.CourseClassroom.CourseId == courseId);
			}

			if (courseClassroomId != null)
			{
				query = query.Where(cs => cs.CourseClassroomId == courseClassroomId);
			}

			if (specialityId != null)
			{
				query = query.Where(cs => cs.ClassroomSpeciality.SpecialityId == specialityId);
			}

			if (classroomSpecialityId != null)
			{
				query = query.Where(cs => cs.ClassroomSpecialityId == classroomSpecialityId);
			}

			return await query.ToListAsync();
		}


		[Authorize]
		[HttpPost]
		public async Task<OkObjectResult> AddAsync(ulong courseClassroomId,
			[FromQuery] HashSet<ulong> classroomSpecialityIds)
		{
			var userId = HttpContext.User.FindFirst(ClaimTypes.NameIdentifier)!.Value;
			var user = await _userService.GetByIdAsync(userId);
			var courseClassroom = await _courseClassroomService.GetAsync(courseClassroomId);
			var member = await _memberService.AuthorizeAsync(courseClassroom.Classroom.Space, user.Id);
			var classroomSpecialities = await _classroomSpecialityService.GetAsync(classroomSpecialityIds);


			var result = await _courseSpecialityService.AddAsync(courseClassroom, classroomSpecialities, member);
			return Ok(result.Item);
		}


		[Authorize]
		[HttpDelete("{courseSpecialityId}")]
		public async Task<OkObjectResult> DeleteAsync(ulong courseSpecialityId)
		{
			var userId = HttpContext.User.FindFirst(ClaimTypes.NameIdentifier)!.Value;
			var user = await _userService.GetByIdAsync(userId);

			var courseSpeciality = await _courseSpecialityService.GetByIdAsync(courseSpecialityId);

			var member = await _memberService.AuthorizeAsync(courseSpeciality.CourseClassroom.Classroom.Space, user.Id);


			var result = await _courseSpecialityService.DeleteAsync(courseSpeciality, member);
			return Ok(result);
		}
	}
}