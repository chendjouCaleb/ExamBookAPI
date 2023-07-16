using System;
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
	
	[Route("api/examination-students")]
	public class ExaminationStudentController:ControllerBase
	{
		private readonly ApplicationDbContext _dbContext;
		private readonly UserService _userService;
		private readonly ExaminationService _examinationService;
		private readonly ExaminationStudentService _examinationStudentService;
		private readonly ParticipantService _participantService;


		[HttpGet("{examinationStudentId}")]
		public async Task<ExaminationStudent> GetAsync(ulong examinationStudentId)
		{
			var examinationStudent = await _dbContext.ExaminationStudents
				.Include(es => es.Participant.Examination)
				.Include(es => es.Participant.ParticipantSpecialities)
				.ThenInclude(ps => ps.ExaminationSpeciality.Speciality)
				.Include(es => es.Student.Space)
				.FirstOrDefaultAsync();

			if (examinationStudent == null)
			{
				throw new ElementNotFoundException("ExaminationStudentNotFoundById", examinationStudentId);
			}

			return examinationStudent;
		}


		[HttpGet]
		public async Task<ICollection<ExaminationStudent>> ListAsync(
			[FromQuery] ulong? studentId, [FromQuery] ulong? examinationId)
		{
			IQueryable<ExaminationStudent> query = _dbContext.ExaminationStudents
				.Include(es => es.Participant.ParticipantSpecialities);

			if (studentId != null)
			{
				query = query
					.Include(es => es.Participant.Examination)
					.Where(es => es.StudentId == studentId);
			}

			if (examinationId == null)
			{
				query = query
					.Include(es => es.Student)
					.Where(es => es.Participant.ExaminationId == examinationId);
			}

			return await query.ToListAsync();
		}


		[HttpPost]
		public async Task<CreatedAtActionResult> AddAsync(ulong examinationId, ulong studentId)
		{
			var userId = HttpContext.User.FindFirst(ClaimTypes.NameIdentifier)!.Value;
			var user = await _userService.GetByIdAsync(userId);
			var examination = await _examinationService.GetByIdAsync(examinationId);


			throw new NotImplementedException();
		}

	}
}