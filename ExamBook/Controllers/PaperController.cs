using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using ExamBook.Entities;
using ExamBook.Exceptions;
using ExamBook.Identity.Entities;
using ExamBook.Identity.Services;
using ExamBook.Models;
using ExamBook.Persistence;
using ExamBook.Services;
using ExamBook.Utils;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ExamBook.Controllers
{
	
	[Route("api/papers")]
	public class PaperController:ControllerBase
	{
		private readonly PaperService _paperService;
		private readonly MemberService _memberService;
		private readonly TestService _testService;
		private readonly UserService _userService;
		private readonly ApplicationDbContext _dbContext;


		public PaperController(PaperService paperService, UserService userService, ApplicationDbContext dbContext, TestService testService)
		{
			_paperService = paperService;
			_userService = userService;
			_dbContext = dbContext;
			_testService = testService;
		}

		[HttpGet("{paperId}")]
		public async Task<Paper> GetAsync(ulong paperId)
		{
			var paper = await _dbContext.Set<Paper>()
				.Include(p => p.PaperScore)
				.Include(p => p.Participant)
				.Include(p => p.Student)
				.Include(p => p.Test)
				.Include(p => p.TestSpeciality)
				.Where(p => p.Id == paperId)
				.FirstOrDefaultAsync();

			if (paper == null)
			{
				throw new ElementNotFoundException("PaperNotFoundById", paperId);
			}

			return paper;
		}


		[HttpGet]
		public async Task<ICollection<Paper>> ListAsync([FromQuery] ulong? studentId,
			[FromQuery] ulong? participantId,
			[FromQuery] ulong? testId)
		{
			IQueryable<Paper> query = _dbContext.Papers
				.Include(p => p.PaperScore)
				.Include(p => p.Test)
				.Include(p => p.Participant)
				.Include(p => p.Student)
				.Include(p => p.Test);

			if (studentId != null)
			{
				query = query.Where(p => p.StudentId == studentId);
			}

			if (participantId != null)
			{
				query = query.Where(p => p.ParticipantId == participantId);
			}
			
			if (testId != null)
			{
				query = query.Where(p => p.TestId == testId);
			}


			return await query.ToListAsync();
		}

		[HttpGet("scores")]
		public async Task<ICollection<PaperScore>> GetScores([FromQuery] ulong? testId,
			[FromQuery] ulong? studentId,
			[FromQuery] ulong? participantId,
			[FromQuery] HashSet<ulong> paperId)
		{
			var userId = HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier)!;
			var user = await _userService.FindByIdAsync(userId);


			var testTeachers = await _dbContext.TestTeachers
				.Include(ct => ct.Member)
				.Include(tt => tt.Test)
				.Where(tt => tt.Member.UserId == user.Id)
				.ToListAsync();
				

			var query = _dbContext.PaperScores
				.Include(p => p.Paper.Test.Space)
				.AsQueryable();
			
			if (testId != null)
			{
				query = query.Where(s => s.Paper.TestId == testId);
			}

			if (studentId != null)
			{
				query = query.Where(s => s.Paper.StudentId == studentId);
			}
			
			if (participantId != null)
			{
				query = query.Where(s => s.Paper.ParticipantId == participantId);
			}

			if (paperId.Any())
			{
				query = query.Where(s => paperId.Contains(s.Id));
			}

			var scores = await query.ToListAsync();

			foreach (var score in scores)
			{
				if (!TakeScore(score, testTeachers))
				{
					score.Value = -1;
				}
			}

			return scores;
		}

		[HttpPut("score")]
		public async Task SetScores([FromBody] PaperScoreModel model)
		{
			var paper = await _paperService.GetByIdAsync(model.PaperId);
			var test = await _testService.GetByIdAsync(paper.Test.Id);
			var userId = HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier)!;
			var user = await _userService.FindByIdAsync(userId);


			await _paperService.SetScoreAsync(test, new List<PaperScoreModel> {model}, user);
		}


		[HttpPost]
		public async Task AddPapers([FromQuery] ulong testId)
		{
			var test = await _testService.GetByIdAsync(testId);
			var userId = HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier)!;
			var user = await _userService.FindByIdAsync(userId);

			if (test.ExaminationId != null)
			{
				await _paperService.AddExaminationTestPapers(test, user);
			}
			else
			{
				await _paperService.AddStudentTestPapersAsync(test, user);
			}
		}


		private bool TakeScore(PaperScore score, List<TestTeacher> testTeachers)
		{
			if (score.Paper.Test.IsPublished)
			{
				return true;
			}
			
			if(testTeachers.Any(tt => tt.TestId == score.Paper.TestId))
			{
				return true;
			}

			return true;
		}
	}
}