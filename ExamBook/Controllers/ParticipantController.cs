using System.Collections.Generic;
using System.Linq;
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


		[HttpGet("{participantId}")]
		public async Task<Participant> GetAsync(ulong participantId)
		{
			var participant = await _dbContext.Participants
				.Include(p => p.Examination)
				.Include(p => p.ExaminationStudent)
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
				
				.Include(p => p.ExaminationStudent)
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
					.Include(p => p.ExaminationStudent!.Student)
					.Where(p => p.ExaminationId == examinationId);
			}

			return await query.ToListAsync();
		}

	}
}