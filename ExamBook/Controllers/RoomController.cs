using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using ExamBook.Entities;
using ExamBook.Helpers;
using ExamBook.Identity.Services;
using ExamBook.Models;
using ExamBook.Persistence;
using ExamBook.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ExamBook.Controllers
{
	[Route("api/rooms")]
	public class RoomController:ControllerBase
	{
		private readonly RoomService _roomService;
		private readonly SpaceService _spaceService;
		private readonly UserService _userService;
		private readonly ApplicationDbContext _dbContext;

		public RoomController(RoomService roomService,
			ApplicationDbContext dbContext, 
			UserService userService, SpaceService spaceService)
		{
			_roomService = roomService;
			_dbContext = dbContext;
			_userService = userService;
			_spaceService = spaceService;
		}


		[HttpGet("{roomId}")]
		public async Task<Room> GetAsync(ulong roomId)
		{
			Room room = await _roomService.GetRoomAsync(roomId);
			return room;
		}

		[HttpGet("contains")]
		public async Task<bool> ContainsAsync([FromQuery] ulong spaceId, [FromQuery] string name)
		{

			if (!string.IsNullOrWhiteSpace(name))
			{
				var normalizedName = StringHelper.Normalize(name);
				return await _dbContext.Rooms
					.Where(c => c.SpaceId == spaceId && c.NormalizedName == normalizedName)
					.AnyAsync();
			}
			
			return false;
		}

		[HttpGet]
		public async Task<List<Room>> ListAsync([FromQuery] ulong spaceId)
		{
			IQueryable<Room> query = _dbContext.Rooms
				.Include(s => s.Space)
				.Where(s => s.SpaceId == spaceId);

			return await query.ToListAsync();
		}
		
		
		[HttpPost]
		[Authorize]
		public async Task<CreatedAtActionResult> AddAsync([FromQuery] ulong spaceId, [FromForm] RoomAddModel model)
		{
			var userId = HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier)!;
			var user = await _userService.FindByIdAsync(userId);
			var space = await _spaceService.GetByIdAsync(spaceId);
			var result = await _roomService.AddRoomAsync(space, model, user);
			var room = result.Item;

			await _dbContext.Rooms.Entry(room).ReloadAsync();
			
			return CreatedAtAction("Get", new {roomId = room.Id}, room);
		}
		
		
		[Authorize]
		[HttpPut("{roomId}/name")]
		public async Task<OkObjectResult> ChangeNameAsync(ulong roomId, [FromBody] IDictionary<string, string> body)
		{
			var userId = HttpContext.User.FindFirst(ClaimTypes.NameIdentifier)!.Value;
			var user = await _userService.GetByIdAsync(userId);
			var room = await _roomService.GetRoomAsync(roomId);
            
			string name = body["name"];
			var model = new RoomChangeNameModel {Name = name};
			var result = await _roomService.ChangeNameAsync(room, model, user);
			return Ok(result);
		}
		
		
		[Authorize]
		[HttpPut("{roomId}/capacity")]
		public async Task<OkObjectResult> ChangeCapacityAsync(ulong roomId, [FromBody] IDictionary<string, string> body)
		{
			var userId = HttpContext.User.FindFirst(ClaimTypes.NameIdentifier)!.Value;
			var user = await _userService.GetByIdAsync(userId);
			var room = await _roomService.GetRoomAsync(roomId);
            
			string capacity = body["capacity"];
			var model = new RoomChangeCapacityModel {Capacity = uint.Parse(capacity)};
			var result = await _roomService.ChangeCapacityAsync(room, model, user);
			return Ok(result);
		}
		
		

		[Authorize]
		[HttpDelete("{roomId}")]
		public async Task<OkObjectResult> DeleteAsync(ulong roomId)
		{
			var userId = HttpContext.User.FindFirst(ClaimTypes.NameIdentifier)!.Value;
			var user = await _userService.GetByIdAsync(userId);
			var room = await _roomService.GetRoomAsync(roomId);

			var result = await _roomService.DeleteAsync(room, user);
			return Ok(result);
		}
	}
}