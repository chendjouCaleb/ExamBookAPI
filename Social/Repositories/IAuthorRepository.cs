using System.Threading.Tasks;
using Social.Entities;

namespace Social.Repositories
{
    public interface IAuthorRepository
    {
        public ValueTask<Author?> FindByIdAsync(string id);
        public Task SaveAsync(Author author);
        public Task UpdateAsync(Author author);
        public Task DeleteAsync(Author author);
    }
}