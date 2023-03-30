using System.Threading.Tasks;
using DriveIO.Models;

namespace DriveIO.Repositories
{
    public interface IFolderRepository
    {
        public Task<Folder?> FindByIdAsync(string id);

        public Task<Folder?> FindByNameAsync(string name);

        public Task<bool> ContainsByNameAsync(string name);

        public Task AddAsync(Folder folder);

        public Task DeleteAsync(Folder folder);
    }
}