using System.Collections.Generic;
using System.Threading.Tasks;
using Social.Entities;

namespace Social.Repositories
{
    public interface IReactionRepository
    {
        ValueTask<Reaction?> GetByIdAsync(long id);
        
        Task SaveAsync(Reaction reaction);

        Task UpdateAsync(Reaction reaction);
        
        Task DeleteAsync(Reaction reaction);

        Task<IEnumerable<Reaction>> GetPostReactions(Post post, string type);

        Task<Reaction?> GetByPostAuthor(Post post, Author author);
        
        Task<bool> ExistsByPostAuthor(Post post, Author author);
    }
}