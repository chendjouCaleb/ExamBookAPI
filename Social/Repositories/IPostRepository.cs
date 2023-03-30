using System.Threading.Tasks;
using Social.Entities;

namespace Social.Repositories
{
    public interface IPostRepository
    {
        ValueTask<Post?> FindAsync(string id);
        Task SaveAsync(Post post);
        Task UpdateAsync(Post post);
        Task DeleteAsync(Post post);
    }
}