using System.Threading.Tasks;
using DriveIO.Models;
using DriveIO.Repositories;

namespace DriveIO.EFCore
{
    public class PictureEFRepository<TContext>:IPictureRepository  where TContext: DriveDbContext
    {
        private readonly TContext _dbContext;

        public PictureEFRepository(TContext dbContext)
        {
            _dbContext = dbContext;
        }

        public ValueTask<Picture?> GetByIdAsync(string id)
        {
            return _dbContext.Pictures.FindAsync(id);
        }
    }
}