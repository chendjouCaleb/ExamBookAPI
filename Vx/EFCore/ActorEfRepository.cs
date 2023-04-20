using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Vx.Models;
using Vx.Repositories;

namespace Vx.EFCore
{
    public class ActorEfRepository<TContext>: IActorRepository where TContext: VxDbContext
    {
        private readonly TContext _dbContext;

        public ActorEfRepository(TContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<Actor?> GetByIdAsync(string id)
        {
            return await _dbContext.Actors
                .Where(s => s.Id == id)
                .FirstOrDefaultAsync();
        }

        public async Task<bool> AnyAsync(string id)
        {
            return await _dbContext.Actors
                .Where(s => s.Id == id)
                .AnyAsync();
        }

        public async Task SaveAsync(Actor actor)
        {
            await _dbContext.Actors.AddAsync(actor);
            await _dbContext.SaveChangesAsync();
        }

        public async Task DeleteAsync(Actor actor)
        {
            _dbContext.Actors.Remove(actor);
            await _dbContext.SaveChangesAsync();
        }
    }
}