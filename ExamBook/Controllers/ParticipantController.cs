using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using ExamBook.Entities;
using ExamBook.Exceptions;
using ExamBook.Identity.Services;
using ExamBook.Persistence;
using ExamBook.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ExamBook.Controllers
{
	
	[Route("api/participants")]
	public class ParticipantController:ControllerBase
	{
		private readonly ApplicationDbContext _dbContext;
		private readonly ParticipantService _participantService;
		private readonly ExaminationService _examinationService;
		private readonly UserService _userService;


		public ParticipantController(ApplicationDbContext dbContext, 
			ParticipantService participantService, 
			ExaminationService examinationService, 
			UserService userService)
		{
			_dbContext = dbContext;
			_participantService = participantService;
			_examinationService = examinationService;
			_userService = userService;
		}

		[HttpGet("{participantId}")]
		public async Task<Participant> GetAsync(ulong participantId)
		{
			var participant = await _dbContext.Participants
				.Include(p => p.Examination)
				.Include(p => p.Student)
				.Include(p => p.ParticipantSpecialities)
				.ThenInclude(ps => ps.ExaminationSpeciality.Speciality)
				.Where(p => p.Id == participantId)
				.FirstOrDefaultAsync();

			if (participant == null)
			{
				throw new ElementNotFoundException("ParticipantNotFoundById", participantId);
			}

			return participant;
		}


		[HttpPost]
		public async Task<List<Participant>> ListAsync(ulong? examinationId, ulong? studentId)
		{
			IQueryable<Participant> query = _dbContext.Participants
				
				.Include(p => p.Student)
				.Include(p => p.ParticipantSpecialities)
				.ThenInclude(ps => ps.ExaminationSpeciality.Speciality);


			if (examinationId != null)
			{
				query = query
					.Include(p => p.Examination)
					.Where(p => p.ExaminationId == examinationId);
			}

			if (studentId != null)
			{
				query = query
					.Include(p => p.Student!)
					.Where(p => p.ExaminationId == examinationId);
			}

			return await query.ToListAsync();
		}


		[HttpPost("add-students")]
		public async Task<ICollection<Participant>> AddAsync([FromQuery] ulong examinationId)
		{
			var userId = HttpContext.User.FindFirst(ClaimTypes.NameIdentifier)!.Value;
			var user = await _userService.GetByIdAsync(userId);
			var examination = await _examinationService.GetByIdAsync(examinationId);

			var students = await _dbContext.Students
				.Where(s => s.SpaceId == examination.SpaceId)
				.ToListAsync();
			var currentParticipants = await _dbContext.Participants
				.Where(s => s.ExaminationId == examinationId)
				.ToListAsync();

			var toAddStudents = students
				.Where(s => currentParticipants.All(p => p.StudentId != s.Id))
				.ToHashSet();
			
			var result = await _participantService.AddAsync(examination, toAddStudents, user);
			var newParticipants = result.Item;

			return newParticipants;
		}
		
		
		
	}
}