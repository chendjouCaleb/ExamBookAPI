using System.Collections.Generic;
using System.Threading.Tasks;
using DriveIO.Models;
using DriveIO.Repositories;
using Microsoft.EntityFrameworkCore;

namespace DriveIO.EFCore
{
    public class PictureEFRepository<TContext>:IPictureRepository  where TContext: DriveDbContext
    {
        private readonly TContext _dbContext;

        public PictureEFRepository(TContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async ValueTask<Picture?> GetByIdAsync(string id)
        {
            return await _dbContext.Pictures
                .Include(p => p.Folder)
                .FirstOrDefaultAsync(p => p.Id == id);
        }

        public async Task<IEnumerable<Picture>> ListAsync()
        {
            return await _dbContext.Pictures.ToListAsync();
        }

        public async Task SaveAsync(Picture picture)
        {
            await _dbContext.AddAsync(picture);
            await _dbContext.SaveChangesAsync();
        }
    }
}