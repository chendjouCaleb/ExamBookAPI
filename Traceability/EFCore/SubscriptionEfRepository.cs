using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Traceability.Models;
using Traceability.Repositories;

namespace Traceability.EFCore
{
    public class SubscriptionEfRepository<TContext>:ISubscriptionRepository where TContext: TraceabilityDbContext
    {
        private readonly TContext _dbContext;

        public SubscriptionEfRepository(TContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<Subscription?> GetByIdAsync(string id)
        {
            return await _dbContext.Subscription
                .Where(s => s.Id == id)
                .FirstOrDefaultAsync();
        }

        public async Task SaveAsync(Subscription subscription)
        {
            await _dbContext.AddAsync(subscription);
            await _dbContext.SaveChangesAsync();
        }

        public async Task UpdateAsync(Subscription subscription)
        {
            _dbContext.Update(subscription);
            await _dbContext.SaveChangesAsync();
        }

        public async Task DeleteAsync(Subscription subscription)
        {
            _dbContext.Subscription.Remove(subscription);
            await _dbContext.SaveChangesAsync();
        }
    }
}