using System;
using System.Threading.Tasks;
using ExamBook.Entities;
using ExamBook.Identity.Models;
using ExamBook.Models;
using ExamBook.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ExamBook.Services
{
    public class MemberService
    {
        private readonly DbContext _dbContext;
        private readonly ILogger<MemberService> _logger;

        public MemberService(DbContext dbContext, ILogger<MemberService> logger)
        {
            _dbContext = dbContext;
            _logger = logger;
        }

        public async Task<Member> AddMember(Space space, MemberAddModel model)
        {
            var user = await _dbContext.Set<User>().FindAsync(model.UserId);
            if (user == null)
            {
                throw new InvalidOperationException($"User with id={model.UserId} not found.");
            }
            if (await IsSpaceMember(space, model.UserId))
            {
                
            }

            Member member = await CreateMember(space, model);
            await _dbContext.AddAsync(member);
            await _dbContext.SaveChangesAsync();
            _logger.LogInformation("New memeber");

            return member;
        }

        public async Task<Member> CreateMember(Space space, MemberAddModel model)
        {
            //var user = await _dbContext.Set<User>().FindAsync(addModel.UserId);
            var member = new Member
            {
                UserId = model.UserId,
                Space = space,
                IsAdmin = model.IsAdmin
            };

            return member;
        }

        public async Task SetAsAdmin(Member member)
        {
            
        }


        public async Task<bool> IsSpaceMember(Space space, string userId)
        {
            return await _dbContext.Set<Member>()
                .AnyAsync(m => m.SpaceId == space.Id && m.UserId == userId);
        }
    }
}