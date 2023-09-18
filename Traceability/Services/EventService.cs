using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Traceability.Models;
using Traceability.Repositories;
using Traceability.Serializers;

namespace Traceability.Services
{
    public class EventService
    {
        private readonly IEventRepository _eventRepository;
        private readonly PublisherService _publisherService;
        private readonly SubjectService _subjectService;
        
        private readonly ActorService _actorService;
        private readonly IDataSerializer _dataSerializer;
        private readonly ILogger<EventService> _logger;

        public EventService(
            IEventRepository eventRepository, 
            ILogger<EventService> logger, 
            IDataSerializer dataSerializer, 
            ActorService actorService, 
            PublisherService publisherService, 
            SubjectService subjectService)
        {
            _eventRepository = eventRepository;
            _logger = logger;
            _dataSerializer = dataSerializer;
            _actorService = actorService;
            _publisherService = publisherService;
            _subjectService = subjectService;
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

        

        public async Task<Event> EmitAsync(ICollection<Publisher> publishers, ICollection<Actor> actors, Subject subject, string name, object data)
        {
            return await EmitAsync(publishers, new[] {subject}, actors, name, data);
        }
        
        
        public async Task<Event> EmitAsync(ICollection<Publisher> publishers,
            ICollection<Subject> subjects, 
            ICollection<Actor> actors, 
            string name, object data)
        {
            if (actors.Count == 0)
            {
                throw new InvalidOperationException("Should provide a least one actor for event.");
            }
            Event @event = new ()
            {
                Name = name,
                DataValue = _dataSerializer.Serialize(data)
            };
            @event.PublisherEvents = CreateEventPublishers(publishers, @event);
            @event.SubjectEvents = CreateEventSubjects(subjects, @event);
            @event.ActorEvents = CreateEventActors(actors, @event);
            
            await _eventRepository.SaveAsync(@event);
            
            _logger.LogInformation("New event");

            return @event;
        }
        
        
        [Obsolete]
        public async Task<Event> EmitAsync(ICollection<Publisher> publishers, Actor actor, string name, object data)
        {
            Event @event = new ()
            {
                Name = name,
                DataValue = _dataSerializer.Serialize(data),
               
            };
            @event.PublisherEvents = CreateEventPublishers(publishers, @event);
            @event.ActorEvents = CreateEventActors(new List<Actor>{actor}, @event);
            
            await _eventRepository.SaveAsync(@event);
            
            _logger.LogInformation("New event");

            return @event;
        }

        public async Task<Event> EmitAsync(
            ICollection<string> publisherIds, 
            ICollection<string> actorIds, 
            string subjectId,
            string name, object data)
        {
            var subject = await _subjectService.GetByIdAsync(subjectId);
            var publishers = await _publisherService.GetSetByIdAsync(publisherIds);
            var actors = await _actorService.GetByIdAsync(actorIds);
            return await EmitAsync(publishers, actors, subject, name, data);
        }
        
        
        
        [Obsolete]
        public async Task<Event> EmitAsync(
            ICollection<string> publisherIds, 
            string actorId, 
            string name, object data)
        {
            var publishers = await _publisherService.GetSetByIdAsync(publisherIds);
            var actor = await _actorService.GetByIdAsync(actorId);
            return await EmitAsync(publishers, actor,  name, data);
        }


        private List<ActorEvent> CreateEventActors(ICollection<Actor> actors, Event @event)
        {
            actors = actors.ToHashSet();
            return actors.Select(actor => new ActorEvent(actor, @event)).ToList();
        }
        
        private List<PublisherEvent> CreateEventPublishers(ICollection<Publisher> publishers, Event @event)
        {
            publishers = publishers.ToHashSet();
            return publishers.Select(publisher => new PublisherEvent(publisher, @event)).ToList();
        }
        
        private List<SubjectEvent> CreateEventSubjects(ICollection<Subject> subjects, Event @event)
        {
            subjects = subjects.ToHashSet();
            return subjects.Select(subject => new SubjectEvent(subject, @event)).ToList();
        }

        public async Task<IEnumerable<Event>> GetSubscriptionEvents(IEnumerable<string> subscriptionIds)
        {
            return await _eventRepository.GetAllAsync(subscriptionIds);
        }



        public async Task<IList<Publisher>> GetPublishers(ICollection<string> publisherIds)
        {
            return await _publisherService.GetSetByIdAsync(publisherIds);
        }
        
        public async Task<IList<Subject>> GetSubjects(ICollection<string> subjectIds)
        {
            return await _subjectService.GetByIdAsync(subjectIds);
        }
        
        public async Task<IList<Actor>> GetActors(ICollection<string> actorIds)
        {
            return await _actorService.GetByIdAsync(actorIds);
        }
    }
    
    
}