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
		private readonly UserService _userService;


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

		//
		// [HttpPost]
		// public async Task<CreatedAtActionResult> AddAsync(
		// 	[FromQuery] ulong spaceId,
		// 	[FromBody] TestAddModel model)
		// {
		// 	AssertHelper.NotNull(model, nameof(model));
		// 	
		// 	var userId = HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier)!;
		// 	var user = await _userService.FindByIdAsync(userId);
		// 	var space = await _spaceService.GetByIdAsync(spaceId);
		//
		// 	var result = await _testService.AddAsync(space, model, user);
		// 	return CreatedAtAction("Get", new {testId = result.Item.Id}, result.Item);
		// }

		
		
	}
}