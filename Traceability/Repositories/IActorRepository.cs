using System.Threading.Tasks;
using Traceability.Models;

namespace Traceability.Repositories
{
    public interface IActorRepository
    {
        Task<Actor?> GetByIdAsync(string id);

        Task<bool> AnyAsync(string id);

        Task SaveAsync(Actor actor);
        Task DeleteAsync(Actor actor);
    }
}