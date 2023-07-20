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
using Traceability.Models;

namespace ExamBook.Controllers
{
	
	[Route("api/members")]
	public class MemberController:ControllerBase
	{
		private readonly MemberService _memberService;
		private readonly SpaceService _spaceService;
		private readonly UserService _userService;
		private readonly ApplicationDbContext _dbContext;


		public MemberController(MemberService memberService,
			ApplicationDbContext dbContext, 
			UserService userService, 
			SpaceService spaceService)
		{
			_memberService = memberService;
			_dbContext = dbContext;
			_userService = userService;
			_spaceService = spaceService;
		}


		[HttpGet("{memberId}")]
		public async Task<Member> GetAsync(ulong memberId)
		{
			return await _memberService.GetByIdAsync(memberId);
		}

		[HttpGet]
		public async Task<ICollection<Member>> ListAsync([FromQuery] MemberSelectModel model)
		{
			IQueryable<Member> query = _dbContext.Set<Member>();

			if (model.SpaceId != 0)
			{
				query = query.Where(m => m.SpaceId == model.SpaceId);
			}

			if (!string.IsNullOrWhiteSpace(model.UserId))
			{
				query = query.Where(m => m.UserId == model.UserId)
					.Include(m => m.Space);
			}

			if (model.IsTeacher != null)
			{
				query = query.Where(m => m.IsTeacher);
			}
			
			if (model.IsAdmin != null)
			{
				query = query.Where(m => m.IsAdmin);
			}

			var members = await query.ToListAsync();
			var userIds = members.Where(m => m.UserId != null).Select(m => m.UserId!).ToHashSet();
			var users = await _userService.ListById(userIds);

			foreach (var member in members)
			{
				member.User = users.Find(u => u.Id == member.UserId);
			}

			return members;
		}

		[HttpPost]
		[Authorize]
		public async Task<CreatedAtActionResult> AddAsync([FromBody] MemberAddModel model, [FromQuery] ulong spaceId)
		{
			AssertHelper.NotNull(model, nameof(model));
			
			var userId = HttpContext.User.FindFirst(ClaimTypes.NameIdentifier)!.Value;
			var user = await _userService.GetByIdAsync(userId);
			var space = await _spaceService.GetByIdAsync(spaceId);

			var result = await _memberService.AddMemberAsync(space, model, user);
			var member = await _memberService.GetByIdAsync(result.Item.Id);
			await _dbContext.Entry(member).ReloadAsync();
			return CreatedAtAction("Get", new {memberId = result.Item.Id}, member);
		}


		
		[HttpPut("{memberId}/admin")]
		[Authorize]
		public async Task ToggleAdminAsync(ulong memberId)
		{
			var userId = HttpContext.User.FindFirst(ClaimTypes.NameIdentifier)!.Value;
			var user = await _userService.GetByIdAsync(userId);
			var member = await _memberService.GetByIdAsync(memberId);
			await _memberService.ToggleAdminAsync(member, user);
		}
		
		
		[HttpPut("{memberId}/teacher")]
		[Authorize]
		public async Task ToggleTeacherAsync(ulong memberId)
		{
			var userId = HttpContext.User.FindFirst(ClaimTypes.NameIdentifier)!.Value;
			var user = await _userService.GetByIdAsync(userId);
			var member = await _memberService.GetByIdAsync(memberId);
			await _memberService.ToggleTeacherAsync(member, user);
		}


		[HttpDelete("{memberId}")]
		[Authorize]
		public async Task<Event> DeleteAsync(ulong memberId)
		{
			var userId = HttpContext.User.FindFirst(ClaimTypes.NameIdentifier)!.Value;
			var user = await _userService.GetByIdAsync(userId);
			var member = await _memberService.GetByIdAsync(memberId);

			return await _memberService.DeleteAsync(member, user);
		}
	}
}