using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DriveIO.Helpers;
using Microsoft.EntityFrameworkCore;
using Social.Entities;
using Social.Repositories;

namespace Social.EFCore
{
    public class AuthorEFRepository<TContext>:IAuthorRepository where TContext: SocialDbContext
    {
        private readonly TContext _dbContext;

        public AuthorEFRepository(TContext dbContext)
        {
            _dbContext = dbContext;
        }

        public ValueTask<Author?> GetByIdAsync(string id)
        {
            return _dbContext.Authors.FindAsync(id);
        }

        public async Task<ICollection<Author>> GetAllAsync()
        {
            return await _dbContext.Authors.ToListAsync();
        }

        public async Task<ICollection<PostFile>> GetPostFilesAsync(Post post)
        {
            return await _dbContext.PostFiles
                .Where(p => p.PostId == post.Id)
                .ToListAsync();
        }

        public async Task SaveAsync(Author author)
        {
            await _dbContext.AddAsync(author);
            await _dbContext.SaveChangesAsync();
        }

        public async Task UpdateAsync(Author author)
        {
            _dbContext.Update(author);
            await _dbContext.SaveChangesAsync();
        }

        public async Task DeleteAsync(Author author)
        {
            _dbContext.Remove(author);
            await _dbContext.SaveChangesAsync();
        }

        public async Task<AuthorSubscription?> GetAuthorSubscriptionAsync(long id)
        {
            return await _dbContext.AuthorSubscriptions
                .Include(a => a.Subscription)
                .Where(a => a.Id == id)
                .FirstOrDefaultAsync();
        }

        public async Task<ICollection<AuthorSubscription>> GetAuthorSubscriptionsAsync(Author author)
        {
            return await _dbContext.AuthorSubscriptions
                .Where(a => a.AuthorId == author.Id)
                .ToListAsync();
        }

        public async Task SaveAuthorSubscriptionAsync(AuthorSubscription authorSubscription)
        {
            await _dbContext.AddAsync(authorSubscription);
            await _dbContext.SaveChangesAsync();
        }

        public async Task DeleteAuthorSubscriptionAsync(AuthorSubscription authorSubscription)
        {
            _dbContext.Remove(authorSubscription);
            await _dbContext.SaveChangesAsync();
        }
    }
}