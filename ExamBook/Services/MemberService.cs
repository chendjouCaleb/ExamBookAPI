using System;
using System.Linq;
using System.Threading.Tasks;
using ExamBook.Entities;
using ExamBook.Exceptions;
using ExamBook.Identity;
using ExamBook.Identity.Entities;
using ExamBook.Identity.Models;
using ExamBook.Identity.Services;
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

        public async Task<Member> GetByIdAsync(ulong memberId)
        {
            var member = await _dbContext.Set<Member>()
                .Include(m => m.Space)
                .Where(m => m.Id == memberId && !m.IsDeleted)
                .FirstOrDefaultAsync();

            if (member == null)
            {
                throw new ElementNotFoundException("MemberNotFoundById", memberId);
            }

            if (!string.IsNullOrWhiteSpace(member.UserId))
            {
                member.User = await _userService.GetByIdAsync(member.UserId);
            }

            return member;
        }

        public async Task<Member> GetOrAddAsync(Space space, string userId, User actor)
        {
            var memberId = await IsSpaceMemberId(space, userId);
            if (memberId != null)
            {
                return await GetByIdAsync(memberId.Value);
            }

            var model = new MemberAddModel { UserId = userId, IsAdmin = false, IsTeacher = false };
            return (await AddMemberAsync(space, model, actor)).Item;
        }


        

        public async Task<ActionResultModel<Member>> AddMemberAsync(Space space, MemberAddModel model, User user)
        {
            var memberUser = await _userService.GetByIdAsync(model.UserId);
            
            if (await IsSpaceMember(space, model.UserId))
            {
                throw new IllegalOperationException("UserIsAlreadyMember");
            }
            
            var publisher = await _publisherService.AddAsync();
            
            Member member = new ()
            {
                UserId = memberUser.Id,
                Space = space,
                IsAdmin = model.IsAdmin,
                IsTeacher = model.IsTeacher,
                PublisherId = publisher.Id
            };
            await _dbContext.AddAsync(member);
            await _dbContext.SaveChangesAsync();

            var actor = await _actorService.GetByIdAsync(user.ActorId);
            var publishers = await _publisherService.GetByIdAsync(space.PublisherId, memberUser.PublisherId);
            publishers = publishers.Add(publisher);
            
            var @event = await _eventService.EmitAsync(publishers, actor, "MEMBER_ADD", member);
            _logger.LogInformation("New member");

            return new ActionResultModel<Member>(member, @event);
        }
        
        
        public async Task<ActionResultModel<Member>> ToggleAdminAsync(Member member, User user)
        {
            AssertHelper.NotNull(member, nameof(member));

            string eventName = member.IsAdmin ? "MEMBER_UNSET_ADMIN" : "MEMBER_SET_ADMIN";
            member.IsAdmin = !member.IsAdmin;
            
            _dbContext.Update(member);
            await _dbContext.SaveChangesAsync();

            var actor = await _actorService.GetByIdAsync(user.ActorId);
            var publishers = await _publisherService.GetByIdAsync(
                member.PublisherId, 
                member.Space!.PublisherId, 
                member.User.PublisherId);

            var @event = await _eventService.EmitAsync(publishers, actor, eventName, new {} );
            _logger.LogInformation("Member set admin");

            return new ActionResultModel<Member>(member, @event);
        }
        
        public async Task<ActionResultModel<Member>> ToggleTeacherAsync(Member member, User user)
        {
            AssertHelper.NotNull(member, nameof(member));

            string eventName = member.IsTeacher ? "MEMBER_UNSET_TEACHER" : "MEMBER_SET_TEACHER";
            member.IsTeacher = !member.IsTeacher;
            
            _dbContext.Update(member);
            await _dbContext.SaveChangesAsync();

            var actor = await _actorService.GetByIdAsync(user.ActorId);
            var publishers = await _publisherService.GetByIdAsync(
                member.PublisherId, 
                member.Space!.PublisherId, 
                member.User.PublisherId);

            var @event = await _eventService.EmitAsync(publishers, actor, eventName, new {} );
            _logger.LogInformation("Member set as teacher");

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
            return await IsSpaceMemberId(space, userId) != null;
        }
        
        public async Task<ulong?> IsSpaceMemberId(Space space, string userId)
        {
            return await _dbContext.Set<Member>()
                .Where(m => m.SpaceId == space.Id && m.UserId == userId && m.DeletedAt == null)
                .Select(m => m.Id)
                .FirstOrDefaultAsync();
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
            AssertHelper.NotNull(member, nameof(member));
            AssertHelper.NotNull(user, nameof(user));
            AssertHelper.NotNull(member.Space, nameof(member.Space));

            var publishersIds = new [] {member.PublisherId, member.Space!.PublisherId};

            member.IsAdmin = false;
            member.DeletedAt = DateTime.UtcNow;
            _dbContext.Update(member);
            await _dbContext.SaveChangesAsync();

            return await _eventService.EmitAsync(publishersIds, user.ActorId, "MEMBER_DELETE", new { });
        }
    }
}