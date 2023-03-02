using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using Vx.Models;

namespace Vx.Components
{
    public class EventEmitter
    {
        private readonly EventDbContext _dbContext;
        private readonly JsonSerializerSettings _settings;
        

        public EventEmitter(EventDbContext dbContext, JsonSerializerSettings settings)
        {
            _dbContext = dbContext;
            _settings = settings;
        }

        public async Task<Event> Emit(IEnumerable<Publisher> publishers, Author author, string name, object data)
        {
            Event @event = new ();
            @event.Author = author;
            @event.Name = name;
            @event.DataValue = JsonConvert.SerializeObject(data, _settings);
            
            await _dbContext.AddAsync(@event);
            
            foreach (var publisher in publishers)
            {
                PublisherEvent publisherEvent = await _CreateEventPublisherAsync(publisher, @event);
                await _dbContext.AddAsync(publisherEvent);
            }

            await _dbContext.SaveChangesAsync();
            
            return @event;
        }


        private async Task<PublisherEvent> _CreateEventPublisherAsync(Publisher publisher, Event @event)
        {
            bool contains = await _dbContext.Set<PublisherEvent>()
                .Where(pe => pe.Publisher == publisher && pe.Event == @event)
                .AnyAsync();

            if (contains)
            {
                throw new InvalidOperationException("Cannot duplicate EventPublisher");
            }
            PublisherEvent publisherEvent = new()
            {
                Publisher = publisher,
                Event = @event
            };

            return publisherEvent;
        }

        private async Task Roolback(Event @event)
        {
            var publishers = _dbContext.Set<PublisherEvent>()
                .Where(pe => pe.Event == @event)
                .ToListAsync();
            _dbContext.RemoveRange(publishers);
            _dbContext.Remove(@event);
            await _dbContext.SaveChangesAsync();
        }
    }
    
    
}