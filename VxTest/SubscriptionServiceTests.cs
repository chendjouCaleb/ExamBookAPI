using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using Traceability.Repositories;
using Traceability.Services;

#pragma warning disable NUnit2005
namespace VxTest
{
    public class SubscriptionServiceTests
    {
        private IServiceProvider _provider = null!;
        private SubscriptionService _subscriptionService = null!;
        private PublisherService _publisherService = null!;
        private ISubscriptionRepository _subscriptionRepository = null!;


        [SetUp]
        public void Setup()
        {
            var services = new ServiceCollection();
            services.Setup();
            _provider = services.BuildServiceProvider();
            _subscriptionService = _provider.GetRequiredService<SubscriptionService>();
            _publisherService = _provider.GetRequiredService<PublisherService>();
            _subscriptionRepository = _provider.GetRequiredService<ISubscriptionRepository>();
        }
        
        [Test]
        public async Task GetSubscription()
        {
            var publisher = await _publisherService.AddAsync();
            var subscriptionId = (await _subscriptionService.SubscribeAsync(publisher)).Id;
            var subscription = (await _subscriptionService.GetByIdAsync(subscriptionId))!;
            
            Assert.NotNull(subscription);
            Assert.NotNull(subscription.Id);
            Assert.NotNull(subscription.CreatedAt);
        }


        [Test]
        public void GetNonExistentSubscriptionShouldThrow()
        {
            var subscriptionId = Guid.NewGuid().ToString();
            var ex = Assert.ThrowsAsync<InvalidOperationException>(async () =>
            {
                await _subscriptionService.GetByIdAsync(subscriptionId);
            })!;

            Assert.AreEqual($"The subscription with id={subscriptionId} not found.", ex.Message);
        }


        [Test]
        public async Task AddSubscription()
        {
            var publisher = await _publisherService.AddAsync();
            var subscription = await _subscriptionService.SubscribeAsync(publisher);
            subscription = (await _subscriptionRepository.GetByIdAsync(subscription.Id))!;

            Assert.NotNull(subscription);
            Assert.IsNotEmpty(subscription.Id);
            Assert.AreEqual(publisher.Id, subscription.PublisherId);
        }


        [Test]
        public async Task DeleteSubscription()
        {
            var publisher = await _publisherService.AddAsync();
            var subscription = await _subscriptionService.SubscribeAsync(publisher);
            subscription = (await _subscriptionRepository.GetByIdAsync(subscription.Id))!;

            await _subscriptionService.DeleteAsync(subscription);
            subscription = (await _subscriptionRepository.GetByIdAsync(subscription.Id))!;
            
            Assert.Null(subscription);
        }
    }
}