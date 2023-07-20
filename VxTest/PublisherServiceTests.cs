using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using Traceability.Repositories;
using Traceability.Services;

#pragma warning disable NUnit2005
namespace VxTest
{
    public class PublisherServiceTests
    {
        private IServiceProvider _provider = null!;
        private PublisherService _publisherService = null!;
        private IPublisherRepository _publisherRepository = null!;


        [SetUp]
        public void Setup()
        {
            var services = new ServiceCollection();
            services.Setup();
            _provider = services.BuildServiceProvider();
            _publisherService = _provider.GetRequiredService<PublisherService>();
            _publisherRepository = _provider.GetRequiredService<IPublisherRepository>();
        }
        
        [Test]
        public async Task GetPublisher()
        {
            var publisherId = (await _publisherService.AddAsync()).Id;
            var publisher = (await _publisherService.GetByIdAsync(publisherId));
            
            Assert.NotNull(publisher);
            Assert.NotNull(publisher.Id);
            Assert.NotNull(publisher.CreatedAt);
        }


        [Test]
        public void GetNonExistentPublisherShouldThrow()
        {
            var publisherId = Guid.NewGuid().ToString();
            var ex = Assert.ThrowsAsync<InvalidOperationException>(async () =>
            {
                await _publisherService.GetByIdAsync(publisherId);
            })!;

            Assert.AreEqual($"Publisher with id={publisherId} not found.", ex.Message);
        }


        [Test]
        public async Task AddPublisher()
        {
            var publisher = await _publisherService.AddAsync();
            publisher = (await _publisherRepository.GetByIdAsync(publisher.Id))!;

            Assert.NotNull(publisher);
            Assert.IsNotEmpty(publisher.Id);
        }


        [Test]
        public async Task DeletePublisher()
        {
            var publisher = await _publisherService.AddAsync();
            publisher = (await _publisherRepository.GetByIdAsync(publisher.Id))!;

            await _publisherService.DeleteAsync(publisher);
            publisher = (await _publisherRepository.GetByIdAsync(publisher.Id))!;
            
            Assert.Null(publisher);
        }
    }
}