using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Traceability.Models;
using Traceability.Repositories;

namespace Traceability.EFCore
{
    public class ActorEfRepository<TContext>: IActorRepository where TContext: TraceabilityDbContext
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

        public ICollection<Actor> GetById(ICollection<string> id)
        {
            return _dbContext.Actors
                .Where(s => id.Contains(s.Id))
                .ToList();
        }

        public async Task<ICollection<Actor>> GetByIdAsync(ICollection<string> id)
        {
            return await _dbContext.Actors
                .Where(s => id.Contains(s.Id))
                .ToListAsync();
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
        
        public async Task SaveAllAsync(ICollection<Actor> actors)
        {
            await _dbContext.AddRangeAsync(actors);
            await _dbContext.SaveChangesAsync();
        }

        public async Task DeleteAsync(Actor actor)
        {
            _dbContext.Actors.Remove(actor);
            await _dbContext.SaveChangesAsync();
        }
    }
}