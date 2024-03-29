﻿using System;
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
using Traceability.Models;

namespace ExamBook.Controllers
{
	
	[Route("api/tests")]
	public class TestController:ControllerBase
	{
		private readonly ApplicationDbContext _dbContext;
		private readonly TestService _testService;
		private readonly SpaceService _spaceService;
		private readonly MemberService _memberService;
		private readonly RoomService _roomService;
		private readonly CourseService _courseService;
		private readonly CourseClassroomService _courseClassroomService;
		private readonly ExaminationService _examinationService;
		private readonly UserService _userService;


		public TestController(ApplicationDbContext dbContext, 
			TestService testService, 
			SpaceService spaceService, 
			UserService userService, 
			ExaminationService examinationService,
			CourseService courseService,
			MemberService memberService, RoomService roomService, CourseClassroomService courseClassroomService)
		{
			_dbContext = dbContext;
			_testService = testService;
			_spaceService = spaceService;
			_userService = userService;
			_examinationService = examinationService;
			_courseService = courseService;
			_memberService = memberService;
			_roomService = roomService;
			_courseClassroomService = courseClassroomService;
		}

		[HttpGet("{testId}")]
		public async Task<Test> GetAsync(ulong testId)
		{
			var test = await _dbContext.Tests
				.Where(t => t.Id == testId)
				.Include(t => t.Room)
				.Include(t => t.CourseClassroom)
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
			[FromQuery] ulong? courseClassroomId,
			[FromQuery] ulong? examinationId)
		{
			IQueryable<Test> query = _dbContext.Tests
				.Include(t => t.Room)
				.Include(t => t.CourseClassroom.Course);

			if (courseClassroomId != null)
			{
				query = query.Where(t => t.CourseClassroom.CourseId == courseClassroomId);
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
		public async Task<CreatedAtActionResult> AddTestCourseAsync(
			[FromQuery] ulong spaceId,
			[FromQuery] ulong courseClassroomId,
			[FromQuery] ulong roomId,
			[FromQuery] HashSet<ulong> memberIds,
			[FromBody] TestAddModel model)
		{
			AssertHelper.NotNull(model, nameof(model));
			
			var userId = HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier)!;
			var user = await _userService.FindByIdAsync(userId);
			var space = await _spaceService.GetByIdAsync(spaceId);
			var courseClassroom = await _courseClassroomService.GetAsync(courseClassroomId);
			var room = await _roomService.GetRoomAsync(roomId);
			var members = await _memberService.ListAsync(memberIds);
			

			var result = await _testService.AddAsync(space, courseClassroom, model, members, room, user);
			return CreatedAtAction("Get", new {testId = result.Item.Id}, result.Item);
		}
		
		

		[HttpPost("test-examinations")]
		public async Task<CreatedAtActionResult> AddTestCourseExaminationAsync(
			[FromQuery] ulong examinationId,
			[FromQuery] ulong courseClassroomId,
			[FromQuery] ulong roomId,
			[FromQuery] HashSet<ulong> memberIds,
			[FromBody] TestAddModel model)
		{
			AssertHelper.NotNull(model, nameof(model));
			
			var userId = HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier)!;
			var user = await _userService.FindByIdAsync(userId);
			var examination = await _examinationService.GetByIdAsync(examinationId);
			var courseClassroom = await _courseClassroomService.GetAsync(courseClassroomId);

			var courseSpecialities = await _dbContext.CourseSpecialities
				.Where(cs => cs.CourseClassroomId == courseClassroom.Id)
				.ToListAsync();
			var specialityIds = courseSpecialities.Select(cs => cs.ClassroomSpecialityId)
				.ToList();

			var examinationSpecialities = await _dbContext.ExaminationSpecialities
				.Include(es => es.Speciality)
				.Where(es => specialityIds.Contains(es.SpecialityId ?? 0))
				.ToListAsync();
			
			var members = await _memberService.ListAsync(memberIds);
			var room = await _roomService.GetRoomAsync(roomId);
			
			var result = await _testService.AddAsync(examination,courseClassroom, model, examinationSpecialities, members, room, user);
			return CreatedAtAction("Get", new {testId = result.Item.Id}, result.Item);
		}
		
		
		[HttpPut("{testId}/coefficient")]
		public async Task<Event> ChangeCoefficientAsync(ulong testId, [FromBody] IDictionary<string, string> body)
		{
			var userId = HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier)!;
			var user = await _userService.FindByIdAsync(userId);
			
			var test = await _testService.GetByIdAsync(testId);
			string coefficientStr = body["coefficient"];

			uint coefficient = uint.Parse(coefficientStr);

			return await _testService.ChangeCoefficientAsync(test, coefficient, user);
		}
		
		[HttpPut("{testId}/duration")]
		public async Task<Event> ChangeDurationAsync(ulong testId, [FromBody] IDictionary<string, string> body)
		{
			var userId = HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier)!;
			var user = await _userService.FindByIdAsync(userId);
			
			var test = await _testService.GetByIdAsync(testId);
			string value = body["duration"];

			uint duration = uint.Parse(value);

			return await _testService.ChangeDurationAsync(test, duration, user);
		}
		
		[HttpPut("{testId}/radical")]
		public async Task<Event> ChangeRadicalAsync(ulong testId, [FromBody] IDictionary<string, string> body)
		{
			var userId = HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier)!;
			var user = await _userService.FindByIdAsync(userId);
			
			var test = await _testService.GetByIdAsync(testId);
			string radicalStr = body["radical"];

			uint radical = uint.Parse(radicalStr);

			return await _testService.ChangeRadicalAsync(test, radical, user);
		}
		
		
		[HttpPut("{testId}/startAt")]
		public async Task<Event> ChangeStartAtAsync(ulong testId, [FromBody] IDictionary<string, string> body)
		{
			var userId = HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier)!;
			var user = await _userService.FindByIdAsync(userId);
			
			var test = await _testService.GetByIdAsync(testId);
			string startAtStr = body["startAt"];

			DateTime startAt = DateTime.Parse(startAtStr);

			return await _testService.ChangeStartAtAsync(test, startAt, user);
		}
		
		
		[HttpPut("{testId}/lock")]
		public async Task<Event> LockAsync(ulong testId)
		{
			var userId = HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier)!;
			var user = await _userService.FindByIdAsync(userId);
			
			var test = await _testService.GetByIdAsync(testId);
			return await _testService.LockAsync(test, user);
		}
		
		[HttpPut("{testId}/unlock")]
		public async Task<Event> UnLockAsync(ulong testId)
		{
			var userId = HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier)!;
			var user = await _userService.FindByIdAsync(userId);
			
			var test = await _testService.GetByIdAsync(testId);
			return await _testService.UnLockAsync(test, user);
		}

		

		[HttpDelete("{testId}")]
		public async Task<NoContentResult> DeleteAsync(ulong testId)
		{
			var userId = HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier)!;
			var user = await _userService.FindByIdAsync(userId);
			
			var test = await _testService.GetByIdAsync(testId);
			await _testService.DeleteAsync(test);

			return NoContent();
		}
		
	}
}