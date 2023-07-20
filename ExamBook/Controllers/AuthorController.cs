using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ExamBook.Persistence;
using ExamBook.Utils;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Social.Entities;
using Social.Services;
using Traceability.Models;
using Traceability.Services;


namespace ExamBook.Controllers
{
    [Route("api/authors")]
    public class AuthorController:ControllerBase
    {
        private readonly ApplicationSocialDbContext _dbContext;
        private readonly AuthorService _authorService;
        private readonly EventService _eventService;

        public AuthorController(ApplicationSocialDbContext dbContext, AuthorService authorService, EventService eventService)
        {
            _dbContext = dbContext;
            _authorService = authorService;
            _eventService = eventService;
        }

        [HttpGet]
        public async Task<IEnumerable<Author>> ListAsync()
        {
            return await _dbContext.Authors.ToListAsync();
        }


        [HttpGet("{id}")]
        public async Task<Author> Get(string id)
        {
            return await _authorService.GetByIdAsync(id);
        }

        [HttpGet("{authorId}/events")]
        public async Task<IEnumerable<Event>> GetEvents(string authorId)
        {
            var author = await _authorService.GetByIdAsync(authorId);
            var authorSubscriptions = await _authorService.GetSubscriptionsAsync(author);

            Console.WriteLine("Subscriptions ids: " + authorSubscriptions.Count());
            var subscriptionIds = authorSubscriptions
                .Where(s => s.SubscriptionId != null)
                .Select(s => s.SubscriptionId!)
                
                .ToList();

            var events = await _eventService.GetSubscriptionEvents(subscriptionIds);

            return events;
        }

        [HttpPost]
        public async Task<Author> AddAsync([FromQuery]string name)
        {
            var author = await _authorService.AddAuthorAsync(name);

            return author;
        }

        [HttpPost("subscribe")]
        public async Task<AuthorSubscription> Subscribe([FromQuery] string selfAuthorId, [FromQuery] string toAuthorId)
        {
            AssertHelper.NotNullOrWhiteSpace(selfAuthorId, nameof(selfAuthorId));
            AssertHelper.NotNullOrWhiteSpace(toAuthorId, nameof(toAuthorId));
            
            var selfAuthor = await _authorService.GetByIdAsync(selfAuthorId);
            var toAuthor = await _authorService.GetByIdAsync(toAuthorId);

            var subscription = await _authorService.SubscribeAsync(selfAuthor, toAuthor);
            return subscription;
        }

        [HttpDelete("unsubscribe")]
        public async Task<NoContentResult> Unsubscribe([FromQuery] long authorSubscriptionId)
        {
            var subscription = await _authorService.GetSubscriptionAsync(authorSubscriptionId);
            await _authorService.UnSubscribeAsync(subscription.Id);
            return NoContent();
        }


        [HttpDelete("{id}")]
        public async Task<NoContentResult> DeleteAsync(string id)
        {
            await _authorService.DeleteAsync(id);
            return NoContent();
        }
    }
}