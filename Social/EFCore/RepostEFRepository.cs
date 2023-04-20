using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Social.Entities;
using Social.Repositories;

namespace Social.EFCore
{
    public class RepostEFRepository<TContext>: IRepostRepository where TContext:SocialDbContext
    {
        private TContext _dbContext;

        public RepostEFRepository(TContext dbContext)
        {
            _dbContext = dbContext;
        }
        
        public async ValueTask<Repost?> GetByIdAsync(long id)
        {
            return await _dbContext.Reposts
                .Where(r => r.Id == id)
                .FirstOrDefaultAsync();
        }

        public async Task SaveAsync(Repost repost)
        {
            await _dbContext.AddAsync(repost);
            await _dbContext.SaveChangesAsync();
        }

        public async Task UpdateAsync(Repost repost)
        {
            _dbContext.Update(repost);
            await _dbContext.SaveChangesAsync();
        }

        public async Task DeleteAsync(Repost repost)
        {
             _dbContext.Remove(repost);
            await _dbContext.SaveChangesAsync();
        }

        public async Task<IEnumerable<Repost>> GetPostReposts(Post post)
        {
            var query = _dbContext.Reposts
                .Where(r => r.ChildPostId == post.Id);
            
            return await query.ToListAsync();
        }


        public async Task<Repost?> GetByPostAuthor(Post post, Author author)
        {
            return await _dbContext.Reposts
                .Where(r => r.ChildPostId == post.Id)
                .Where(r => r.AuthorId == author.Id)
                .FirstOrDefaultAsync();
        }
        
        public async Task<bool> ExistsByPostAuthor(Post post, Author author)
        {
            return await _dbContext.Reposts
                .Where(r => r.ChildPostId == post.Id)
                .Where(r => r.AuthorId == author.Id)
                .AnyAsync();
        }
    }
}