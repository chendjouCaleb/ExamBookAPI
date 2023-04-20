using System.Collections.Generic;
using System.Threading.Tasks;
using DriveIO.Models;

namespace DriveIO.Repositories
{
    public interface IPictureRepository
    {
        public ValueTask<Picture?> GetByIdAsync(string id);
        public Task<IEnumerable<Picture>> ListAsync();
        public Task SaveAsync(Picture picture);
    }
}