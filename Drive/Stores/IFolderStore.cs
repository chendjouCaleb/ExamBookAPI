using System.Threading.Tasks;

namespace DriveIO.Stores
{
    public interface IFolderStore
    {
        public Task CreateAsync(string name);

        public Task<bool> ContainsAsync(string name);

        public Task DeleteAsync(string name);
    }
}