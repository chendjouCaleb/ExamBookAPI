using System.Collections.Generic;
using System.Threading.Tasks;
using DriveIO.Models;

namespace DriveIO.Repositories
{
    public interface IFileRepository
    {
        Task<BaseFile?> GetByIdAsync(string id);

        Task<BaseFile?> GetByNameAsync(string name);

        Task SaveAsync(BaseFile baseFile);

        Task<IEnumerable<BaseFile>> ListAsync();
    }
}