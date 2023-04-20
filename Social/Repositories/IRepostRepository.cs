using System.Collections.Generic;
using System.Threading.Tasks;
using Social.Entities;

namespace Social.Repositories
{
    public interface IRepostRepository
    {
        ValueTask<Repost?> GetByIdAsync(long id);
        
        Task SaveAsync(Repost repost);

        Task UpdateAsync(Repost repost);
        
        Task DeleteAsync(Repost repost);

        Task<IEnumerable<Repost>> GetPostReposts(Post post);

        Task<Repost?> GetByPostAuthor(Post post, Author author);
        
        Task<bool> ExistsByPostAuthor(Post post, Author author);
    }
}