using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using ExamBook.Entities;
using ExamBook.Identity.Services;
using ExamBook.Models;
using ExamBook.Persistence;
using ExamBook.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ExamBook.Controllers
{
	[Route("api/specialities")]
	public class SpecialityController:ControllerBase
	{
		private readonly SpecialityService _specialityService;
		private readonly SpaceService _spaceService;
		private readonly UserService _userService;
		private readonly ApplicationDbContext _dbContext;

		public SpecialityController(SpecialityService specialityService,
			ApplicationDbContext dbContext, 
			UserService userService, SpaceService spaceService)
		{
			_specialityService = specialityService;
			_dbContext = dbContext;
			_userService = userService;
			_spaceService = spaceService;
		}


		[HttpGet("{specialityId}")]
		public async Task<Speciality> GetAsync(ulong specialityId)
		{
			Speciality speciality = await _specialityService.GetAsync(specialityId);
			return speciality;
		}


		[HttpGet]
		public async Task<List<Speciality>> ListAsync([FromQuery] ulong spaceId)
		{
			IQueryable<Speciality> query = _dbContext.Specialities
				.Include(s => s.Space)
				.Where(s => s.SpaceId == spaceId);

			return await query.ToListAsync();
		}
		
		
		[HttpPost]
		[Authorize]
		public async Task<CreatedAtActionResult> AddAsync([FromQuery] ulong spaceId, [FromBody] SpecialityAddModel model)
		{
			var userId = HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier)!;
			var user = await _userService.FindByIdAsync(userId);
			var space = await _spaceService.GetByIdAsync(spaceId);
			var result = await _specialityService.AddSpecialityAsync(space, model, user);
			var speciality = result.Item;

			await _dbContext.Specialities.Entry(speciality).ReloadAsync();
			
			return CreatedAtAction("Get", new {specialityId = speciality.Id}, speciality);
		}
		
		
		[Authorize]
		[HttpPut("{specialityId}/name")]
		public async Task<OkObjectResult> ChangeNameAsync(ulong specialityId, [FromBody] IDictionary<string, string> body)
		{
			var userId = HttpContext.User.FindFirst(ClaimTypes.NameIdentifier)!.Value;
			var user = await _userService.GetByIdAsync(userId);
			var speciality = await _specialityService.GetAsync(specialityId);
            
			string name = body["name"];
			var result = await _specialityService.ChangeNameAsync(speciality, name, user);
			return Ok(result);
		}
		
		
		[Authorize]
		[HttpPut("{specialityId}/description")]
		public async Task<OkObjectResult> ChangeDescriptionAsync(ulong specialityId, [FromBody] IDictionary<string, string> body)
		{
			var userId = HttpContext.User.FindFirst(ClaimTypes.NameIdentifier)!.Value;
			var user = await _userService.GetByIdAsync(userId);
			var speciality = await _specialityService.GetAsync(specialityId);
            
			string description = body["description"];
			var result = await _specialityService.ChangeDescriptionAsync(speciality, description, user);
			return Ok(result);
		}


		[Authorize]
		[HttpDelete("{specialityId}")]
		public async Task<OkObjectResult> DeleteAsync(ulong specialityId)
		{
			var userId = HttpContext.User.FindFirst(ClaimTypes.NameIdentifier)!.Value;
			var user = await _userService.GetByIdAsync(userId);
			var speciality = await _specialityService.GetAsync(specialityId);

			var result = await _specialityService.DeleteAsync(speciality, user);
			return Ok(result);
		}
	}
}