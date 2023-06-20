using System.Collections.Generic;
using System.Threading.Tasks;
using Vx.Models;

namespace Vx.Repositories
{
    public interface IEventRepository
    {
        Task SaveAsync(Event @event);
        Task SaveAsync(Event @event, IEnumerable<PublisherEvent> publisherEvents);
        Task DeleteAsync(Event @event);

        Task<ICollection<Event>> GetAllAsync(IEnumerable<string> subscriptionIds, long firstId = 0);
        
        Task<Event?> GetByIdAsync(long id);

        Task<ICollection<PublisherEvent>> GetEventPublishers(Event action);

        Task SavePublisherEventsAsync(IEnumerable<PublisherEvent> publisherEvents);

        Task<bool> IsPublisherAsync(Event @event, Publisher publisher);
    }
}