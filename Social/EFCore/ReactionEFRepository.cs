using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Social.Entities;
using Social.Repositories;

namespace Social.EFCore
{
    public class ReactionEFRepository<TContext>: IReactionRepository where TContext:SocialDbContext
    {
        private TContext _dbContext;

        public ReactionEFRepository(TContext dbContext)
        {
            _dbContext = dbContext;
        }
        
        public async ValueTask<Reaction?> GetByIdAsync(long id)
        {
            return await _dbContext.Reactions
                .Where(r => r.Id == id)
                .FirstOrDefaultAsync();
        }

        public async Task SaveAsync(Reaction reaction)
        {
            await _dbContext.AddAsync(reaction);
            await _dbContext.SaveChangesAsync();
        }

        public async Task UpdateAsync(Reaction reaction)
        {
            _dbContext.Update(reaction);
            await _dbContext.SaveChangesAsync();
        }

        public async Task DeleteAsync(Reaction reaction)
        {
             _dbContext.Remove(reaction);
            await _dbContext.SaveChangesAsync();
        }

        public async Task<IEnumerable<Reaction>> GetPostReactions(Post post, string type)
        {
            var query = _dbContext.Reactions
                .Where(r => r.PostId == post.Id);

            if (!string.IsNullOrWhiteSpace(type))
            {
                query = query.Where(r => r.Type == type);
            }

            return await query.ToListAsync();
        }


        public async Task<Reaction?> GetByPostAuthor(Post post, Author author)
        {
            return await _dbContext.Reactions
                .Where(r => r.PostId == post.Id)
                .Where(r => r.AuthorId == author.Id)
                .FirstOrDefaultAsync();
        }
        
        public async Task<bool> ExistsByPostAuthor(Post post, Author author)
        {
            return await _dbContext.Reactions
                .Where(r => r.PostId == post.Id)
                .Where(r => r.AuthorId == author.Id)
                .AnyAsync();
        }
    }
}