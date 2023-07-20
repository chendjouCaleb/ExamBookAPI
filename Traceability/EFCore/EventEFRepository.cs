using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Traceability.Models;
using Traceability.Repositories;

namespace Traceability.EFCore
{
    public class EventEFRepository<TContext>: IEventRepository where TContext: TraceabilityDbContext
    {
        private readonly TContext _dbContext;

        public EventEFRepository(TContext dbContext)
        {
            _dbContext = dbContext;
        }
        

        public async Task<Event?> GetByIdAsync(long id)
        {
            return await _dbContext.Events.Where(e => e.Id == id).FirstOrDefaultAsync();
        }

        public async Task<ICollection<PublisherEvent>> GetEventPublishers(Event action)
        {
            return await _dbContext.PublisherEvents
                .Include(pe => pe.Event)
                .Where(pe => pe.EventId == action.Id)
                .ToListAsync();
        }

        public async Task<ICollection<Event>> GetAllAsync(IEnumerable<string> subscriptionIds, long firstId = 0)
        {
            var subscriptions = await _dbContext.Set<Subscription>()
                .Where(s => subscriptionIds.Contains(s.Id))
                .ToListAsync();
            
            var ids = subscriptions.Select(s => s.PublisherId);
            var publisherEvents = await _dbContext.PublisherEvents
                .Include(pe => pe.Event)
                .Where(pe => ids.Contains(pe.PublisherId))
                .Where(pe => pe.EventId > firstId)
                .ToListAsync();
                
            var events = publisherEvents.DistinctBy(pe => pe.EventId)
                .Select(pe => pe.Event!)
                .ToList();

            return events;
        }

        public async Task SaveAsync(Event @event)
        {
            await _dbContext.AddAsync(@event);
            await _dbContext.SaveChangesAsync();
        }

        public async Task SaveAsync(Event @event, IEnumerable<PublisherEvent> publisherEvents)
        {
            await _dbContext.Events.AddAsync(@event);
            await _dbContext.PublisherEvents.AddRangeAsync(publisherEvents);
            await _dbContext.SaveChangesAsync();
        }

        public async Task DeleteAsync(Event @event)
        {
            _dbContext.Remove(@event);
            await _dbContext.SaveChangesAsync();
        }

        public async Task SavePublisherEventsAsync(IEnumerable<PublisherEvent> publisherEvents)
        {
            await _dbContext.AddRangeAsync(publisherEvents);
            await _dbContext.SaveChangesAsync();
        }

        public async Task<bool> IsPublisherAsync(Event @event, Publisher publisher)
        {
            return await _dbContext.Set<PublisherEvent>()
                .AnyAsync(pe => pe.EventId == @event.Id && pe.PublisherId == publisher.Id);
        }
    }
}