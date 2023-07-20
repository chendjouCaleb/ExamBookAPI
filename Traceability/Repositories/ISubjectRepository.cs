using System.Collections.Generic;
using System.Threading.Tasks;
using Traceability.Models;

namespace Traceability.Repositories
{
    public interface ISubjectRepository
    {
        Subject? GetById(string id);
        Task<Subject?> GetByIdAsync(string id);

        ICollection<Subject> GetById(ICollection<string> id);
        Task<ICollection<Subject>> GetByIdAsync(ICollection<string> id);

        Task SaveAsync(Subject subject);
        Task SaveAllAsync(ICollection<Subject> subjects);

        Task DeleteAsync(Subject subject);
    }
}