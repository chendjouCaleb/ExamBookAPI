using System.Collections.Generic;
using System.Threading.Tasks;
using DriveIO.Models;
using DriveIO.Repositories;
using Microsoft.EntityFrameworkCore;

namespace DriveIO.EFCore
{
    public class FileEFRepository<TContext>:IFileRepository where TContext:DriveDbContext
    {
        private TContext _dbContext;


        public FileEFRepository(TContext dbContext)
        {
            _dbContext = dbContext;
        }

        public Task<BaseFile?> GetByIdAsync(string id)
        {
            return _dbContext.Files.Include(f => f.Folder)
                .FirstOrDefaultAsync(f => f.Id == id);
        }

        public async Task<BaseFile?> GetByNameAsync(string name)
        {
            string normalizedName = name.Normalize().ToUpper();
            return await _dbContext.Files
                .Include(f => f.Folder)
                .FirstOrDefaultAsync(f => f.NormalizedName == normalizedName);
        }

        public async Task SaveAsync(BaseFile baseFile)
        {
            await _dbContext.AddAsync(baseFile);
            await _dbContext.SaveChangesAsync();
        }

        public async Task<IEnumerable<BaseFile>> ListAsync()
        {
            return await _dbContext.Files
                .Include(f => f.Folder)
                .ToListAsync();
        }
    }
}