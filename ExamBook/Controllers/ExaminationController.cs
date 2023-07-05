using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using ExamBook.Entities;
using ExamBook.Identity.Services;
using ExamBook.Models;
using ExamBook.Persistence;
using ExamBook.Services;
using ExamBook.Utils;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ExamBook.Controllers
{
	
	[Route("api/examinations")]
	public class ExaminationController:ControllerBase
	{
		private readonly ExaminationService _examinationService;
		private readonly ApplicationDbContext _dbContext;
		private readonly UserService _userService;
		private readonly SpaceService _spaceService;

		public ExaminationController(ApplicationDbContext dbContext, 
			ExaminationService examinationService, 
			SpaceService spaceService, 
			UserService userService)
		{
			_dbContext = dbContext;
			_examinationService = examinationService;
			_spaceService = spaceService;
			_userService = userService;
		}

		[HttpGet("{examinationId}")]
		public async Task<Examination> GetAsync(ulong examinationId)
		{
			var examination = await _examinationService.GetByIdAsync(examinationId);
			examination.ExaminationSpecialities = await _dbContext.ExaminationSpecialities
				.Where(es => es.ExaminationId == examinationId)
				.ToListAsync();

			return examination;
		}


		[HttpPost]
		public async Task<CreatedAtActionResult> AddAsync([FromQuery] ulong spaceId,
			[FromBody] ExaminationAddModel model)
		{
			AssertHelper.NotNull(model, nameof(model));
			var userId = HttpContext.User.FindFirst(ClaimTypes.NameIdentifier)!.Value;
			var user = await _userService.GetByIdAsync(userId);
			var space = await _spaceService.GetByIdAsync(spaceId);

			var result = await _examinationService.AddAsync(space, model, user);
			var examination = result.Item;
			await _dbContext.Entry(examination).ReloadAsync();

			return CreatedAtAction("Get", new {examinationId = examination.Id}, examination);
		}


		[Authorize]
		[HttpPut("{examinationId}/name")]
		public async Task<OkObjectResult> ChangeNameAsync(ulong examinationId, [FromBody] IDictionary<string, string> body)
		{
			var userId = HttpContext.User.FindFirst(ClaimTypes.NameIdentifier)!.Value;
			var user = await _userService.GetByIdAsync(userId);
			var examination = await _examinationService.GetByIdAsync(examinationId);
            
			string name = body["name"];
			var result = await _examinationService.ChangeNameAsync(examination, name, user);
			return Ok(result);
		}
		
		
		
	}
}