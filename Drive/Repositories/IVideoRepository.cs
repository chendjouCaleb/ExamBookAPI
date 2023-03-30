using System.Threading.Tasks;
using DriveIO.Models;

namespace DriveIO.Repositories
{
    public interface IVideoRepository
    {
        public Task<Video?> GetByIdAsync(string id);
    }
}