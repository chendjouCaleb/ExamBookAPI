using System.Collections.Generic;
using System.Threading.Tasks;
using Social.Entities;

namespace Social.Repositories
{
    public interface IAuthorRepository
    {
        public ValueTask<Author?> GetByIdAsync(string id);

        public Task<IEnumerable<Author>> GetAllAsync();

        public Task<IEnumerable<PostFile>> GetPostFilesAsync(Post post);
        public Task SaveAsync(Author author);
        public Task UpdateAsync(Author author);
        public Task DeleteAsync(Author author);

        public Task<AuthorSubscription?> GetAuthorSubscriptionAsync(long id);
        public Task<IEnumerable<AuthorSubscription>> GetAuthorSubscriptionsAsync(Author author);
        public Task SaveAuthorSubscriptionAsync(AuthorSubscription authorSubscription);
        public Task DeleteAuthorSubscriptionAsync(AuthorSubscription authorSubscription);
    }
}