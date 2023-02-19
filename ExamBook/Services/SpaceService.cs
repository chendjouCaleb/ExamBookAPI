using System;
using System.Linq;
using System.Threading.Tasks;
using ExamBook.Entities;
using ExamBook.Models;
using Microsoft.EntityFrameworkCore;

namespace ExamBook.Services
{
    public class SpaceService
    {
        private readonly DbContext _dbContext;
        public readonly MemberService _memberService;

        
        public async Task<Space> GetAsync(string identifier)
        {
            Space? space = await _dbContext.Set<Space>()
                .FirstOrDefaultAsync(s => s.Identifier.Equals(identifier, StringComparison.OrdinalIgnoreCase));

            if (space == null)
            {
                throw new NullReferenceException();
            }
            return space;
        }

        public async Task<Space> AddAsync(User user, SpaceModel model)
        {
            if (await AnyAsync(model.Identifier))
            {
                
                throw new InvalidOperationException($"The identifier '{model.Identifier}' is used.");
            }

            Space space = new()
            {
                Identifier = model.Identifier,
                Name = model.Name
            };
            await _dbContext.AddAsync(space);

            MemberAddModel adminAddModel = new (){ IsAdmin = true, UserId = user.Id };
            Member admin = await _memberService.CreateMember(space, adminAddModel);

            await _dbContext.AddAsync(admin);
            await _dbContext.SaveChangesAsync();

            return space;
        }


        public async Task ChangeIdentifier(Space space, ChangeSpaceIdentifierModel model)
        {
            if (await AnyAsync(model.Identifier))
            {
                
            }

            space.Identifier = model.Identifier;
            _dbContext.Update(space);
            await _dbContext.SaveChangesAsync();
        }
        
        
        public async Task ChangeInfo(Space space, SpaceChangeInfoModel model)
        {
            space.Name = model.Name;
            _dbContext.Update(space);
            await _dbContext.SaveChangesAsync();
        }

        
        public async Task Delete(Space space)
        {
            await Destroy(space);
        }

        

        public async Task<bool> AnyAsync(string identifier)
        {
            return await _dbContext.Set<Space>().AnyAsync(s => s.Identifier == identifier);
        }


        public async Task MarkAsDeleted(Space space)
        {
            space.DeletedAt  = DateTime.Now;
            _dbContext.Update(space);
            await _dbContext.SaveChangesAsync();
        }

        public async Task Destroy(Space space)
        {
            var members = _dbContext.Set<Member>().Where(m => m.SpaceId == space.Id);
            
            _dbContext.RemoveRange(members);
            _dbContext.Remove(space);
            await _dbContext.SaveChangesAsync();
        }
    }
}