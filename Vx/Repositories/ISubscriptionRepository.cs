using System;
using System.Threading.Tasks;
using Vx.Models;

namespace Vx.Repositories
{
    public interface ISubscriptionRepository
    {
        Task<Subscription?> GetByIdAsync(string id);

        Task SaveAsync(Subscription subscription);

        Task UpdateAsync(Subscription subscription);

        Task DeleteAsync(Subscription subscription);
    }
}