using System.Threading.Tasks;
using DriveIO.Models;

namespace DriveIO.Repositories
{
    public interface IPictureRepository
    {
        public ValueTask<Picture?> GetByIdAsync(string id);
    }
}