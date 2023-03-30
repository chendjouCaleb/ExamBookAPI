using System.Threading.Tasks;
using DriveIO.Helpers;
using DriveIO.Models;
using DriveIO.Repositories;
using Microsoft.EntityFrameworkCore;

namespace DriveIO.EFCore
{
    public class FolderEFRepository<TContext>:IFolderRepository where TContext:DriveDbContext
    {

        private readonly TContext _dbContext;

        public FolderEFRepository(TContext dbContext)
        {
            _dbContext = dbContext;
        }


        public async Task<Folder?> FindByIdAsync(string id)
        {
            return await _dbContext.Folders.FindAsync(id);
        }

        public async Task<Folder?> FindByNameAsync(string name)
        {
            var normalizedName = StringHelper.Normalize(name);
            return await _dbContext.Folders
                .FirstOrDefaultAsync(f => f.NormalizedName == normalizedName);
        }

        public async Task<bool> ContainsByNameAsync(string name)
        {
            var normalizedName = StringHelper.Normalize(name);
            return await _dbContext.Folders
                .AnyAsync(f => f.NormalizedName == normalizedName);
        }

        public async Task AddAsync(Folder folder)
        {
            await _dbContext.AddAsync(folder);
            await _dbContext.SaveChangesAsync();
        }

        public async Task DeleteAsync(Folder folder)
        {
            _dbContext.Remove(folder);
            await _dbContext.SaveChangesAsync();
        }
    }
}