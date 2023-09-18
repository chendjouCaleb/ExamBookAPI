using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Traceability.Models;
using Traceability.Serializers;
using Traceability.Services;

namespace Traceability.Asserts
{
    public class EventAssertions
    {
        private readonly IDataSerializer _serializer;
        private readonly PublisherService _publisherService;
        private readonly SubjectService _subjectService;
        private readonly ActorService _actorService;
        public Event Event { get; set; }
        public IEnumerable<PublisherEvent> PublisherEvents { get; set; }
        public IEnumerable<ActorEvent> ActorEvents { get; set; }
        public IEnumerable<SubjectEvent> SubjectEvents { get; set; }

        public EventAssertions(Event @event, IServiceProvider provider)
        {
            Event = @event;
            PublisherEvents = Event.PublisherEvents;
            ActorEvents = Event.ActorEvents;
            SubjectEvents = Event.SubjectEvents;
            _serializer = provider.GetRequiredService<IDataSerializer>();
            _publisherService = provider.GetRequiredService<PublisherService>();
            _actorService = provider.GetRequiredService<ActorService>();
            _subjectService = provider.GetRequiredService<SubjectService>();
        }

        public EventAssertions HasPublisher(Publisher publisher)
        {
            var any = PublisherEvents.Any(pe => pe.PublisherId == publisher.Id);

            if (!any)
            {
                throw new EventAssertionException($"The event has no publisher with id={publisher.Id}");
            }

            return this;
        }
        
        public EventAssertions HasSubject(Subject subject)
        {
            var any = SubjectEvents.Any(pe => pe.SubjectId == subject.Id);

            if (!any)
            {
                throw new EventAssertionException($"The event has no subject with id={subject.Id}");
            }

            return this;
        }
        
        
        public async Task<EventAssertions> HasPublisherIdAsync(string publisherId)
        {
            var publisher = await _publisherService.GetByIdAsync(publisherId);
            return HasPublisher(publisher);
        }

        public EventAssertions HasActor(Actor actor)
        {
            var any = ActorEvents.Any(pe => pe.ActorId == actor.Id);

            if (!any)
            {
                throw new EventAssertionException($"The event has no actor with id={actor.Id}");
            }

            return this;
        }
        
        public async Task<EventAssertions> HasSubjectIdAsync(string subjectId)
        {
            var subject = await _subjectService.GetByIdAsync(subjectId);
            return HasSubject(subject);
        }
        
        public async Task<EventAssertions> HasActorIdAsync(string actorId)
        {
            var actor = await _actorService.GetByIdAsync(actorId);
            return HasActor(actor);
        }

        public EventAssertions HasName(string name)
        {
            if (Event.Name != name)
            {
                throw new EventAssertionException($"Expected event name: {name}; actual={Event.Name}.");
            }

            return this;
        }

        public EventAssertions HasData(object data)
        {
            var dataValue = _serializer.Serialize(data);
            if (dataValue != Event.DataValue)
            {
                throw new EventAssertionException($"Expected data: {dataValue};\n actual={Event.DataValue}.");
            }

            return this;
        }
    }
}