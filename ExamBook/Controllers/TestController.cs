using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using ExamBook.Entities;
using ExamBook.Exceptions;
using ExamBook.Identity.Services;
using ExamBook.Models;
using ExamBook.Persistence;
using ExamBook.Services;
using ExamBook.Utils;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ExamBook.Controllers
{
	
	[Route("api/tests")]
	public class TestController:ControllerBase
	{
		private readonly ApplicationDbContext _dbContext;
		private readonly TestService _testService;
		private readonly SpaceService _spaceService;
		private readonly MemberService _memberService;
		private readonly CourseService _courseService;
		private readonly ExaminationService _examinationService;
		private readonly ExaminationSpecialityService _examinationSpecialityService;
		private readonly UserService _userService;
		private readonly SpecialityService _specialityService;


		public TestController(ApplicationDbContext dbContext, 
			TestService testService, 
			SpaceService spaceService, 
			UserService userService, 
			SpecialityService specialityService, ExaminationService examinationService, 
			ExaminationSpecialityService examinationSpecialityService,
			CourseService courseService,
			MemberService memberService)
		{
			_dbContext = dbContext;
			_testService = testService;
			_spaceService = spaceService;
			_userService = userService;
			_specialityService = specialityService;
			_examinationService = examinationService;
			_examinationSpecialityService = examinationSpecialityService;
			_courseService = courseService;
			_memberService = memberService;
		}

		[HttpGet("{testId}")]
		public async Task<Test> GetAsync(ulong testId)
		{
			var test = await _dbContext.Tests
				.Where(t => t.Id == testId)
				.Include(t => t.Course)
				.Include(t => t.Space)
				.Include(t => t.Examination)
				.FirstOrDefaultAsync();

			if (test == null)
			{
				throw new ElementNotFoundException("TestNotFoundById", testId);
			}

			return test;
		}

		[HttpGet]
		public async Task<List<Test>> ListAsync(
			[FromQuery] ulong? spaceId, 
			[FromQuery] ulong? courseId,
			[FromQuery] ulong? examinationId)
		{
			IQueryable<Test> query = _dbContext.Tests
				.Include(t => t.Course);

			if (courseId != null)
			{
				query = query.Where(t => t.CourseId == courseId);
			}

			if (examinationId != null)
			{
				query = query.Where(t => t.ExaminationId == examinationId);
			}

			if (spaceId != null)
			{
				query = query.Where(t => t.SpaceId == spaceId);
			}

			return await query.ToListAsync();
		}

		

		[HttpPost]
		public async Task<CreatedAtActionResult> AddAsync(
			[FromQuery] ulong spaceId,
			[FromQuery] HashSet<ulong> specialityIds,
			[FromQuery] HashSet<ulong> memberIds,
			[FromBody] TestAddModel model)
		{
			AssertHelper.NotNull(model, nameof(model));
			
			var userId = HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier)!;
			var user = await _userService.FindByIdAsync(userId);
			var space = await _spaceService.GetByIdAsync(spaceId);
			var specialities = await _specialityService.ListAsync(specialityIds);
			var members = await _memberService.ListAsync(memberIds);

			var result = await _testService.AddAsync(space, model, specialities, user);
			return CreatedAtAction("Get", new {testId = result.Item.Id}, result.Item);
		}

		
		
		[HttpPost("test-courses")]
		public async Task<CreatedAtActionResult> AddTestCourseAsync(
			[FromQuery] ulong spaceId,
			[FromQuery] ulong courseId,
			[FromBody] TestAddModel model)
		{
			AssertHelper.NotNull(model, nameof(model));
			
			var userId = HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier)!;
			var user = await _userService.FindByIdAsync(userId);
			var space = await _spaceService.GetByIdAsync(spaceId);
			var course = await _courseService.GetCourseAsync(courseId);

			var result = await _testService.AddAsync(space, course, model, user);
			return CreatedAtAction("Get", new {testId = result.Item.Id}, result.Item);
		}
		
		
		[HttpPost("test-examinations")]
		public async Task<CreatedAtActionResult> AddTestExaminationAsync(
			[FromQuery] ulong examinationId,
			[FromQuery] HashSet<ulong> examinationSpecialityIds,
			[FromBody] TestAddModel model)
		{
			AssertHelper.NotNull(model, nameof(model));
			
			var userId = HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier)!;
			var user = await _userService.FindByIdAsync(userId);
			var examination = await _examinationService.GetByIdAsync(examinationId);
			var space = examination.Space;
			var examinationSpecialities = await _examinationSpecialityService
				.ListAsync(examinationSpecialityIds);

			var result = await _testService.AddAsync(examination, model, examinationSpecialities, user);
			return CreatedAtAction("Get", new {testId = result.Item.Id}, result.Item);
		}
		
		
		[HttpPost("test-examination-courses")]
		public async Task<CreatedAtActionResult> AddTestCourseExaminationAsync(
			[FromQuery] ulong examinationId,
			[FromQuery] ulong courseId,
			[FromQuery] HashSet<ulong> examinationSpecialityIds,
			[FromBody] TestAddModel model)
		{
			AssertHelper.NotNull(model, nameof(model));
			
			var userId = HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier)!;
			var user = await _userService.FindByIdAsync(userId);
			var examination = await _examinationService.GetByIdAsync(examinationId);
			var course = await _courseService.GetCourseAsync(courseId);
			var space = examination.Space;
			var examinationSpecialities = await _examinationSpecialityService
				.ListAsync(examinationSpecialityIds);

			var result = await _testService.AddAsync(examination,course, model, examinationSpecialities, user);
			return CreatedAtAction("Get", new {testId = result.Item.Id}, result.Item);
		}
		
	}
}