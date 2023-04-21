using System;
using System.Threading.Tasks;
using ExamBook.Entities;
using ExamBook.Exceptions;
using ExamBook.Identity;
using ExamBook.Identity.Models;
using ExamBook.Models;
using ExamBook.Utils;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Vx.Models;
using Vx.Services;

namespace ExamBook.Services
{
    public class MemberService
    {
        private readonly DbContext _dbContext;
        private readonly UserService _userService;
        private readonly ActorService _actorService;
        private readonly PublisherService _publisherService;
        private readonly EventService _eventService;
        private readonly ILogger<MemberService> _logger;

        public MemberService(DbContext dbContext, 
            ILogger<MemberService> logger,
            PublisherService publisherService, 
            EventService eventService,
            UserService userService,
            ActorService actorService)
        {
            _dbContext = dbContext;
            _logger = logger;
            _publisherService = publisherService;
            _eventService = eventService;
            _userService = userService;
            _actorService = actorService;
        }

        public async Task<ActionResultModel<Member>> AddMemberAsync(Space space, MemberAddModel model, User user)
        {
            var memberUser = await _userService.FindByIdAsync(model.UserId);
            if (memberUser == null)
            {
                throw new InvalidOperationException($"User with id={model.UserId} not found.");
            }
            
            if (await IsSpaceMember(space, model.UserId))
            {
                throw new IllegalOperationException("UserIsAlreadyMember");
            }
            
            var publisher = await _publisherService.AddAsync();
            
            Member member = new ()
            {
                UserId = model.UserId,
                Space = space,
                IsAdmin = model.IsAdmin,
                PublisherId = publisher.Id
            };
            await _dbContext.AddAsync(member);
            await _dbContext.SaveChangesAsync();

            var actor = await _actorService.GetByIdAsync(user.ActorId);
            var spacePublisher = await _publisherService.GetByIdAsync(space.PublisherId);
            var publishers = new [] { publisher, spacePublisher };

            var @event = await _eventService.EmitAsync(publishers, actor, "MEMBER_ADD", member);
            _logger.LogInformation("New member");

            return new ActionResultModel<Member>(member, @event);
        }

        public Member NewMember(Space space, MemberAddModel model)
        {
            //var user = await _dbContext.Set<User>().FindAsync(addModel.UserId);
            var member = new Member
            {
                UserId = model.UserId,
                Space = space,
                IsAdmin = model.IsAdmin
            };

            return member;
        }


        public async Task<bool> IsSpaceMember(Space space, string userId)
        {
            return await _dbContext.Set<Member>()
                .AnyAsync(m => m.SpaceId == space.Id && m.UserId == userId && m.DeletedAt == null);
        }

        public async Task<Publisher> GetPublisherAsync(Member member)
        {
            if (string.IsNullOrWhiteSpace(member.PublisherId))
            {
                throw new IllegalStateException("MemberHasNoPublisher");
            }
            
            return await _publisherService.GetByIdAsync(member.PublisherId);
        }

        public async Task<Event> DeleteAsync(Member member, User user)
        {
            Asserts.NotNull(member, nameof(member));
            Asserts.NotNull(user, nameof(user));
            Asserts.NotNull(member.Space, nameof(member.Space));

            var publishersIds = new [] {member.PublisherId, member.Space!.PublisherId};

            member.IsAdmin = false;
            member.DeletedAt = DateTime.UtcNow;
            _dbContext.Update(member);
            await _dbContext.SaveChangesAsync();

            return await _eventService.EmitAsync(publishersIds, user.ActorId, "MEMBER_DELETE", new { });
        }
    }
}