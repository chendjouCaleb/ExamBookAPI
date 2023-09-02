using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Social.Entities;
using Social.Repositories;
using Traceability.Models;
using Traceability.Services;

namespace Social.Services
{
    public class AuthorService
    {
        private readonly IAuthorRepository _authorRepository;
        private readonly ActorService _actorService;
        private readonly PublisherService _publisherService;
        private readonly SubscriptionService _subscriptionService;
        private readonly ILogger<AuthorService> _logger;

        public AuthorService(IAuthorRepository authorRepository, ILogger<AuthorService> logger, ActorService actorService, SubscriptionService subscriptionService, PublisherService publisherService)
        {
            _authorRepository = authorRepository;
            _logger = logger;
            _actorService = actorService;
            _subscriptionService = subscriptionService;
            _publisherService = publisherService;
        }


        public async Task<Author> AddAuthorAsync(string name)
        {
            var emitter = _actorService.Create("AUTHOR_ACTOR");
            var publisher = await _publisherService.AddAsync();
            var author = new Author
            {
                Name = name,
                ActorId = emitter.Id,
                Actor = emitter,
                
                PublisherId = publisher.Id,
                Publisher = publisher
            };
            await _authorRepository.SaveAsync(author);
            await _actorService.SaveAsync(emitter);
            await SubscribeAsync(author, author);
            _logger.LogInformation("New author; id={}", author.Name);
            return author;
        }

        public async Task<Publisher> AddAuthorPublisherAsync(Author author)
        {
            if (!string.IsNullOrEmpty(author.PublisherId))
            {
                throw new InvalidOperationException(
                    $"The author id={author.Id}, Name={author.Name} already have a publisher.");
            }
            
            var publisher = await _publisherService.AddAsync();
            author.PublisherId = publisher.Id;
            author.Publisher = publisher;
            await _authorRepository.UpdateAsync(author);

            return publisher;
        }
        
        public async Task<Actor> AddAuthorActorAsync(Author author)
        {
            if (!string.IsNullOrEmpty(author.ActorId))
            {
                throw new InvalidOperationException(
                    $"The author id={author.Id}, Name={author.Name} already have a actor.");
            }

            var actor = _actorService.Create("AUTHOR_ACTOR");
            author.ActorId = actor.Id;
            author.Actor = actor;
            await _authorRepository.UpdateAsync(author);
            await _actorService.SaveAsync(actor);

            return actor;
        }
        

        public async Task EnsureAuthorsHasPublisher()
        {
            var authors = await _authorRepository.GetAllAsync();
            foreach (var author in authors)
            {
                if (string.IsNullOrWhiteSpace(author.PublisherId))
                {
                    await AddAuthorPublisherAsync(author);
                }
            }
        }
        
        public async Task EnsureAuthorSelfSubscribe()
        {
            var authors = await _authorRepository.GetAllAsync();
            foreach (var author in authors)
            {
                await SubscribeAsync(author, author);
            }
        }
        
        
        public async Task EnsureAuthorsHasActor()
        {
            var authors = await _authorRepository.GetAllAsync();
            foreach (var author in authors)
            {
                if (string.IsNullOrWhiteSpace(author.ActorId))
                {
                    await AddAuthorActorAsync(author);
                }
            }
        }

        public async Task<Author> GetByIdAsync(string id)
        {
            var author = await _authorRepository.GetByIdAsync(id);

            if (author == null)
            {
                throw new InvalidOperationException($"Author with id={id} not found.");
            }

            return author;
        }

        public async Task<AuthorSubscription> GetSubscriptionAsync(long id)
        {
            var authorSubscription = await _authorRepository.GetAuthorSubscriptionAsync(id);
            if (authorSubscription == null)
            {
                throw new InvalidOperationException($"AuthorSubscription with id={id} not found.");
            }

            return authorSubscription;
        }


        public async Task<ICollection<AuthorSubscription>> GetSubscriptionsAsync(Author author)
        {
            var authorSubscriptions = await _authorRepository.GetAuthorSubscriptionsAsync(author);
            return authorSubscriptions;
        }

        public async Task<AuthorSubscription> SubscribeAsync(Author self, Author to)
        { 
            var publisher = await _publisherService.GetByIdAsync(to.PublisherId);
            var subscription = await _subscriptionService.SubscribeAsync(publisher);
            var authorSubscription = new AuthorSubscription
            {
                Author = self,
                Subscription = subscription,
                SubscriptionId = subscription.Id
            };
            await _authorRepository.SaveAuthorSubscriptionAsync(authorSubscription);
            return authorSubscription;
        }


        public async Task UnSubscribeAsync(long authorSubscriptionId)
        {
            var authorSubscription = await _authorRepository.GetAuthorSubscriptionAsync(authorSubscriptionId);
            var subscription = await _subscriptionService.GetByIdAsync(authorSubscription!.SubscriptionId!);

            await _subscriptionService.DeleteAsync(subscription!);
        }

        public async Task DeleteAsync(string id)
        {
            var author = await GetByIdAsync(id);
            await _authorRepository.DeleteAsync(author);
            _logger.LogInformation("Delete author");
        }
        
        public async Task DeleteAsync(Author author)
        {
            await _authorRepository.DeleteAsync(author);
            _logger.LogInformation("Delete author");
        }
        
        
    }
}