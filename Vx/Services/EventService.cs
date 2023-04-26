using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Vx.Models;
using Vx.Repositories;
using Vx.Serializers;

namespace Vx.Services
{
    public class EventService
    {
        private readonly IEventRepository _eventRepository;
        private readonly PublisherService _publisherService;
        private readonly ActorService _actorService;
        private readonly IDataSerializer _dataSerializer;
        private readonly ILogger<EventService> _logger;

        public EventService(
            IEventRepository eventRepository, 
            ILogger<EventService> logger, 
            IDataSerializer dataSerializer, ActorService actorService, PublisherService publisherService)
        {
            _eventRepository = eventRepository;
            _logger = logger;
            _dataSerializer = dataSerializer;
            _actorService = actorService;
            _publisherService = publisherService;
        }

        public async Task<Event> GetByIdAsync(long id)
        {
            var action = await _eventRepository.GetByIdAsync(id);

            if (action == null)
            {
                throw new InvalidOperationException($"The event with id={id} not found.");
            }

            return action;
        }

        

        public async Task<Event> EmitAsync(IEnumerable<Publisher> publishers, Actor actor, string name, object data)
        {
            
            if (!publishers.Any())
            {
                throw new InvalidOperationException("Cannot create event without at least one publisher.");
            }
            Event @event = new ();
            @event.Actor = actor;
            @event.Name = name;
            @event.DataValue = _dataSerializer.Serialize(data);

            var publisherEvents = new List<PublisherEvent>();
            foreach (var publisher in publishers)
            {
                PublisherEvent publisherEvent = await _CreateEventPublisherAsync(publisher, @event);
                publisherEvents.Add(publisherEvent);
            }
            
            
            await _eventRepository.SaveAsync(@event, publisherEvents);
            
            _logger.LogInformation("New event");

            return @event;
        }

        public async Task<Event> EmitAsync(Publisher publisher, Actor actor, string name, object data)
        {
            return await EmitAsync(new[] {publisher}, actor, name, data);
        }

        public async Task<Event> EmitAsync(ICollection<string> publisherIds, string actorId, string name, object data)
        {
            var publishers = await _publisherService.GetByIdAsync(publisherIds);
            var actor = await _actorService.GetByIdAsync(actorId);
            return await EmitAsync(publishers, actor, name, data);
        }

        

        private async Task<PublisherEvent> _CreateEventPublisherAsync(Publisher publisher, Event @event)
        {
            if (await _eventRepository.IsPublisherAsync(@event, publisher))
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

        public async Task<IEnumerable<Event>> GetSubscriptionEvents(IEnumerable<string> subscriptionIds)
        {
            return await _eventRepository.GetAllAsync(subscriptionIds, 0);
        }
    }
    
    
}