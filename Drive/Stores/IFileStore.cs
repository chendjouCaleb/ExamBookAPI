using System.IO;
using System.Threading.Tasks;
using DriveIO.Models;

namespace DriveIO.Stores
{
    public interface IFileStore
    {
        public Task WriteFileAsync(Stream fileStream, BaseFile file);
        public Task DeleteAsync(BaseFile file);
    }
}