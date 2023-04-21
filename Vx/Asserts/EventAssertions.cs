using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Vx.Models;
using Vx.Serializers;

namespace Vx.Asserts
{
    public class EventAssertions
    {
        private IDataSerializer _serializer;
        public Event Event { get; set; }
        public IEnumerable<PublisherEvent> PublisherEvents { get; set; }

        public EventAssertions(Event @event, IServiceProvider provider)
        {
            Event = @event;
            PublisherEvents = Event.PublisherEvents;
            _serializer = provider.GetRequiredService<IDataSerializer>();
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

        public EventAssertions HasActor(Actor actor)
        {
            if (Event.ActorId != actor.Id)
            {
                throw new EventAssertionException($"The actor[id={actor.Id}] is not actor of this event.");
            }

            return this;
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
                throw new EventAssertionException($"Expected data: {data}; actual={Event.DataValue}.");
            }

            return this;
        }
    }
}