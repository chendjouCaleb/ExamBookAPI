using System.Threading.Tasks;
using DriveIO.Models;
using DriveIO.Repositories;
using Microsoft.EntityFrameworkCore;

namespace DriveIO.EFCore
{
    public class VideoEFRepository<TContext>:IVideoRepository  where TContext: DriveDbContext
    {
        private readonly TContext _dbContext;

        public VideoEFRepository(TContext dbContext)
        {
            _dbContext = dbContext;
        }

        public Task<Video?> GetByIdAsync(string id)
        {
            return _dbContext.Videos
                .Include(f => f.Folder)
                .FirstOrDefaultAsync(f => f.Id == id);
        }
    }
}