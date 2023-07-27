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
	
	[Route("api/examination-specialities")]
	public class ExaminationSpecialityController:ControllerBase
	{
		private readonly ApplicationDbContext _dbContext;
		private readonly SpecialityService _specialityService;
		private readonly ExaminationService _examinationService;
		private readonly UserService _userService;
		private readonly ExaminationSpecialityService _service;


		public ExaminationSpecialityController(ApplicationDbContext dbContext, 
			SpecialityService specialityService, 
			ExaminationService examinationService, 
			UserService userService, 
			ExaminationSpecialityService service)
		{
			_dbContext = dbContext;
			_specialityService = specialityService;
			_examinationService = examinationService;
			_userService = userService;
			_service = service;
		}

		[HttpGet("{examinationSpecialityId}")]
		public async Task<ExaminationSpeciality> GetAsync(ulong examinationSpecialityId)
		{
			var examinationSpeciality = await _dbContext.Set<ExaminationSpeciality>()
				.Include(es => es.Examination.Space)
				.Include(es => es.Speciality)
				.Where(es => es.Id == examinationSpecialityId)
				.FirstOrDefaultAsync();

			if (examinationSpeciality == null)
			{
				throw new ElementNotFoundException("ExaminationSpecialityNotFoundById", examinationSpecialityId);
			}

			return examinationSpeciality;
		}


		[HttpGet]
		public async Task<ICollection<ExaminationSpeciality>> ListAsync(
			[FromQuery] ulong? examinationId,
			[FromQuery] ulong? specialityId)
		{
			var query = _dbContext.ExaminationSpecialities.AsQueryable();

			if (examinationId != null)
			{
				query = query.Include(es => es.Speciality)
					.Where(es => es.ExaminationId == examinationId);
			}
			
			if (specialityId != null)
			{
				query = query.Include(es => es.Examination)
					.Where(es => es.SpecialityId == specialityId);
			}

			return await query.ToListAsync();
		}


		[HttpPost]
		public async Task<CreatedAtActionResult> AddAsync([FromQuery] ulong examinationId, [FromQuery] ulong specialityId)
		{
			var userId = HttpContext.User.FindFirst(ClaimTypes.NameIdentifier)!.Value;
			var user = await _userService.GetByIdAsync(userId);
			var speciality = await _specialityService.GetAsync(specialityId);
			var examination = await _examinationService.GetByIdAsync(examinationId);

			var result = await _service.AddAsync(examination, speciality, user);
			var examinationSpeciality = result.Item;

			return CreatedAtAction("Get", new {examinationSpecialityId = result.Item.Id}, examinationSpeciality);
		}

		
	}
}