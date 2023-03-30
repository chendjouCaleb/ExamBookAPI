using System.Threading.Tasks;
using Social.Entities;
using Social.Repositories;

namespace Social.EFCore
{
    public class AuthorEFRepository<TContext>:IAuthorRepository where TContext: SocialDbContext
    {
        private TContext _dbContext;

        public AuthorEFRepository(TContext dbContext)
        {
            _dbContext = dbContext;
        }

        public ValueTask<Author?> FindByIdAsync(string id)
        {
            return _dbContext.Authors.FindAsync(id);
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
    }
}