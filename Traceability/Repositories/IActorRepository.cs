using System.Collections.Generic;
using System.Threading.Tasks;
using Traceability.Models;

namespace Traceability.Repositories
{
    public interface IActorRepository
    {
        Task<Actor?> GetByIdAsync(string id);
        
        ICollection<Actor> GetById(ICollection<string> id);
        Task<ICollection<Actor>> GetByIdAsync(ICollection<string> id);

        Task<bool> AnyAsync(string id);

        Task SaveAsync(Actor actor);
        Task SaveAllAsync(ICollection<Actor> actors);
        Task DeleteAsync(Actor actor);
    }
}