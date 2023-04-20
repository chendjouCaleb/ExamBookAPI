using System;
using System.Threading.Tasks;
using Vx.Models;

namespace Vx.Repositories
{
    public interface IPublisherRepository
    {
        Task<Publisher?> GetByIdAsync(string id);

        Task SaveAsync(Publisher publisher);

        Task UpdateAsync(Publisher publisher);

        Task Delete(Publisher publisher);
    }
}