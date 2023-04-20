using System;
using System.Threading.Tasks;
using Vx.Models;
using Vx.Repositories;

namespace Vx.Services
{
    public class SubscriptionService
    {
        private readonly ISubscriptionRepository _subscriptionRepository;

        public SubscriptionService(ISubscriptionRepository subscriptionRepository)
        {
            _subscriptionRepository = subscriptionRepository;
        }


        public async Task<Subscription?> GetByIdAsync(string id)
        {
            var subscription = await _subscriptionRepository.GetByIdAsync(id);
            if (subscription == null)
            {
                throw new InvalidOperationException($"The subscription with id={id} not found.");
            }

            return subscription;
        }

        public async Task<Subscription> SubscribeAsync(Publisher publisher)
        {
            Subscription subscription = new()
            {
                Publisher = publisher
            };

            await _subscriptionRepository.SaveAsync(subscription);
            return subscription;
        }

        public async Task DeleteAsync(Subscription subscription)
        {
            await _subscriptionRepository.DeleteAsync(subscription);
        }
    }
}