using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using NUnit.Framework;
using Vx.Repositories;
using Vx.Services;

#pragma warning disable NUnit2005
namespace VxTest
{
    
    
    public class EventServiceTests
    {
        private IServiceProvider _provider = null!;
        private EventService _eventService = null!;
        private PublisherService _publisherService = null!;
        private ActorService _actorService = null!;
        private IEventRepository _eventRepository = null!;


        [SetUp]
        public void Setup()
        {
            var services = new ServiceCollection();
            services.Setup();
            _provider = services.BuildServiceProvider();
            _eventService = _provider.GetRequiredService<EventService>();
            _publisherService = _provider.GetRequiredService<PublisherService>();
            _actorService = _provider.GetRequiredService<ActorService>();
            _eventRepository = _provider.GetRequiredService<IEventRepository>();
        }


        [Test]
        public async Task AddEvent()
        {
            var actor = await _actorService.AddAsync();
            var publisher = await _publisherService.AddAsync();
            var data = new {Value = 10};
            var action = await _eventService.EmitAsync(new[] {publisher}, actor, "EVENT_NAME", data);

            action = await _eventService.GetByIdAsync(action.Id);
            var publisherEvents = await _eventRepository.GetEventPublishers(action);
            
            Assert.NotNull(action);
            Assert.AreEqual(actor.Id, action.ActorId);
            Assert.AreEqual("EVENT_NAME", action.Name);
            Assert.AreEqual(JsonConvert.SerializeObject(data), action.DataValue);
            Assert.AreEqual(1, publisherEvents.Count());

            foreach (var publisherEvent in publisherEvents)
            {
                Assert.AreEqual(action.Id, publisherEvent.EventId);
                Assert.AreEqual(publisher.Id, publisherEvent.PublisherId);
            }
        }
    }
}