using System.Threading.Tasks;
using Social.Entities;

namespace Social.Repositories
{
    public interface IPostRepository
    {
        ValueTask<Post?> FindAsync(long id);
        Task SaveAsync(Post post);

        Task SavePostFileAsync(PostFile postFile);
        Task UpdateAsync(Post post);
        Task DeleteAsync(Post post);
    }
}