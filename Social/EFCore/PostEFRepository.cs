using System.Threading.Tasks;
using Social.Entities;
using Social.Repositories;

namespace Social.EFCore
{
    public class PostEFRepository<TContext>:IPostRepository where TContext: SocialDbContext
    {
        private TContext _dbContext;

        public PostEFRepository(TContext dbContext)
        {
            _dbContext = dbContext;
        }

        public ValueTask<Post?> FindAsync(long id)
        {
            return _dbContext.Posts.FindAsync(id);
        }

        public async Task SaveAsync(Post post)
        {
            await _dbContext.AddAsync(post);
            await _dbContext.SaveChangesAsync();
        }

        public async Task SavePostFileAsync(PostFile postFile)
        {
            await _dbContext.AddAsync(postFile);
            await _dbContext.SaveChangesAsync();
        }

        public async Task UpdateAsync(Post post)
        {
            _dbContext.Update(post);
            await _dbContext.SaveChangesAsync();
        }

        public async Task DeleteAsync(Post post)
        {
            _dbContext.Remove(post);
            await _dbContext.SaveChangesAsync();
        }
    }
}