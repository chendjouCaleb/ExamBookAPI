using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using Traceability.Repositories;
using Traceability.Services;

#pragma warning disable NUnit2005
namespace VxTest
{
    public class ActorServiceTests
    {
        private IServiceProvider _provider = null!;
        private ActorService _actorService = null!;
        private IActorRepository _actorRepository = null!;


        [SetUp]
        public void Setup()
        {
            var services = new ServiceCollection();
            services.Setup();
            _provider = services.BuildServiceProvider();
            _actorService = _provider.GetRequiredService<ActorService>();
            _actorRepository = _provider.GetRequiredService<IActorRepository>();
        }

        [Test]
        public async Task GetActor()
        {
            var actorId = (await _actorService.AddAsync()).Id;
            var actor = await _actorService.GetByIdAsync(actorId);
            
            Assert.NotNull(actor);
            Assert.NotNull(actor.Id);
            Assert.NotNull(actor.CreatedAt);
        }


        [Test]
        public void GetNonExistentActorShouldThrow()
        {
            var actorId = Guid.NewGuid().ToString();
            var ex = Assert.ThrowsAsync<InvalidOperationException>(async () =>
            {
                await _actorService.GetByIdAsync(actorId);
            })!;

            Assert.AreEqual($"Actor with id={actorId} not found.", ex.Message);
        }

        [Test]
        public async Task AddActor()
        {
            var actor = await _actorService.AddAsync();
            actor = await _actorRepository.GetByIdAsync(actor.Id);
            
            Assert.NotNull(actor!);
            Assert.NotNull(actor!.Id);
            Assert.NotNull(actor.CreatedAt);
        }

        [Test]
        public async Task DeleteActor()
        {
            var actor = await _actorService.AddAsync();
            await _actorService.DeleteAsync(actor);

            actor = await _actorRepository.GetByIdAsync(actor.Id);
            Assert.Null(actor);
        }
    }
}