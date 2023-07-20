using System.Threading.Tasks;
using Traceability.Models;

namespace Traceability.Repositories
{
    public interface ISubscriptionRepository
    {
        Task<Subscription?> GetByIdAsync(string id);

        Task SaveAsync(Subscription subscription);

        Task UpdateAsync(Subscription subscription);

        Task DeleteAsync(Subscription subscription);
    }
}