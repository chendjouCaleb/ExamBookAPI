using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Social.Services;
using Vx.Repositories;

namespace SocialTest
{
    public class AuthorTests
    {
        private IServiceProvider _provider = null!;
        private AuthorService _authorService = null!;
        private IActorRepository _actorRepository = null!;
        
        [SetUp]
        public void Setup()
        {
            var services = new ServiceCollection();


            _provider = services.Setup().BuildServiceProvider();
            _authorService = _provider.GetRequiredService<AuthorService>();
            _actorRepository = _provider.GetRequiredService<IActorRepository>();
        }

        [Test]
        public async Task AddAuthor()
        {
            var author = await _authorService.AddAuthorAsync("name");
            var actor = await _actorRepository.GetByIdAsync(author.ActorId);
            
            
            Assert.That(author, Is.Not.Null);
            Assert.That(author.Name, Is.EqualTo("name"));
            Assert.That(author.DeletedAt, Is.Null);
            Assert.NotNull(actor);
            Assert.AreEqual(author.ActorId, actor!.Id);
        }


        [Test]
        public async Task AddSubscription()
        {
            var self = await _authorService.AddAuthorAsync("name");
            var to = await _authorService.AddAuthorAsync("name1");

            var authorSubscription = await _authorService.SubscribeAsync(self, to);
            var subscription = authorSubscription.Subscription;
            
            Assert.AreEqual(self.Id, authorSubscription.AuthorId);
            Assert.AreEqual(subscription!.Id, authorSubscription.SubscriptionId);
            
            Assert.AreEqual(to.PublisherId, subscription.Publisher!.Id);
            
            

        }
    }
}