using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Traceability.Models;
using Traceability.Repositories;

namespace Traceability.EFCore
{
    public class PublisherEFRepository <TContext>: IPublisherRepository where TContext: TraceabilityDbContext
    {
        private readonly TContext _dbContext;


        public PublisherEFRepository(TContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<Publisher?> GetByIdAsync(string id)
        {
            return await _dbContext.Publishers
                .Where(s => s.Id == id)
                .FirstOrDefaultAsync();
        }
        
        public Publisher? GetById(string id)
        {
            return _dbContext.Publishers
                .FirstOrDefault(s => s.Id == id);
        }

        public async Task<ICollection<Publisher>> GetByIdAsync(ICollection<string> id)
        {
            return await _dbContext.Publishers
                .Where(s => id.Contains(s.Id))
                .ToListAsync();
        }

        public async Task SaveAsync(Publisher publisher)
        {
            await _dbContext.AddAsync(publisher);
            await _dbContext.SaveChangesAsync();
        }

        public async Task SaveAllAsync(ICollection<Publisher> publisher)
        {
            await _dbContext.AddRangeAsync(publisher);
            await _dbContext.SaveChangesAsync();
        }

        public async Task UpdateAsync(Publisher publisher)
        {
            _dbContext.Update(publisher);
            await _dbContext.SaveChangesAsync();
        }

        public async Task Delete(Publisher publisher)
        {
            _dbContext.Remove(publisher);
            await _dbContext.SaveChangesAsync();
        }
    }
}