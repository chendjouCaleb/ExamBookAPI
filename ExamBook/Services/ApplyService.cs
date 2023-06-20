using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using ExamBook.Entities;
using ExamBook.Exceptions;
using ExamBook.Helpers;
using ExamBook.Identity.Entities;
using ExamBook.Identity.Services;
using ExamBook.Models;
using ExamBook.Models.Data;
using ExamBook.Utils;
using Microsoft.EntityFrameworkCore;
using Vx.Models;
using Vx.Services;

namespace ExamBook.Services
{
    public class ApplyService
    {
        private readonly DbContext _dbContext;
        private readonly EventService _eventService;
        private readonly UserService _userService;
        private readonly MemberService _memberService;
        private readonly StudentService _studentService;
        private readonly PublisherService _publisherService;

        public ApplyService(DbContext dbContext, 
            EventService eventService, 
            PublisherService publisherService, 
            UserService userService, 
            MemberService memberService, StudentService studentService)
        {
            _dbContext = dbContext;
            _eventService = eventService;
            _publisherService = publisherService;
            _userService = userService;
            _memberService = memberService;
            _studentService = studentService;
        }

        public async Task<Apply> GetByIdAsync(ulong applyId)
        {
            var apply = await _dbContext.Set<Apply>()
                .Include(s => s.Space)
                .Where(s => s.Id == applyId)
                .FirstOrDefaultAsync();

            if (apply == null)
            {
                throw new ElementNotFoundException("ApplyNotFoundById", applyId);
            }
            apply.User = await _userService.GetByIdAsync(apply.UserId);
            
            return apply;
        }


        public async Task<ActionResultModel<Apply>> AddAsync(Space space, ApplyAddModel model, User actor)
        {
            AssertHelper.NotNull(space, nameof(space));
            AssertHelper.NotNull(model, nameof(model));

            
            if (await ContainsAsync(space, model.Code))
            {
                throw new UsedValueException("ApplyCodeUsed");
            }
            
            string normalizedCode = model.Code.Normalize().ToUpper();
            
            var user = await _userService.GetByIdAsync(model.UserId);

            var publisher = await _publisherService.AddAsync();
            Apply apply = new()
            {
                FirstName = model.FirstName,
                LastName = model.LastName,
                BirthDate = model.BirthDate,
                Sex = model.Sex,
                NormalizedCode = normalizedCode,
                Code = model.Code,
                Space = space,
                SpaceId = space.Id,
                PublisherId = publisher.Id
            };
            var publisherIds = new List<string> { space.PublisherId, user.PublisherId }
                .ToImmutableList();

            var specialities = await _dbContext.Set<Speciality>()
                .Where(s => model.SpecialityIds.Contains(s.Id))
                .Include(s => s.Space)
                .ToListAsync();

            var applySpecialities = new List<ApplySpeciality>();
            foreach (var speciality in specialities)
            {
                AssertHelper.IsTrue(speciality.SpaceId == space.Id);
                var applySpeciality = new ApplySpeciality(apply, speciality);
                applySpecialities.Add(applySpeciality);
            }
            
            
            await _dbContext.AddAsync(apply);
            await _dbContext.AddRangeAsync(applySpecialities);
            await _dbContext.SaveChangesAsync();

            
            publisherIds = publisherIds.AddRange(specialities.Select(s => s.PublisherId));
            
            var @event = await _eventService.EmitAsync(publisherIds, user.ActorId, "APPLY_ADD", apply);
            
            return new ActionResultModel<Apply>(apply, @event);
        }


        public async Task<Event> AcceptAsync(Apply apply, ApplyAcceptModel model, User actor)
        {
            AssertHelper.NotNull(apply, nameof(apply));
            AssertHelper.NotNull(apply.Space, nameof(apply.Space));
            AssertHelper.NotNull(actor, nameof(actor));
            AssertHelper.NotNull(model, nameof(model));

            var specialities = await _dbContext.Set<ApplySpeciality>()
                .Where(s => s.ApplyId == apply.Id)
                .ToListAsync();

            if (await _studentService.ContainsAsync(apply.Space, model.Code))
            {
                throw new UsedValueException("StudentCodeUsed");
            }

            var studentAddModel = new StudentAddModel
            {
                UserId = apply.UserId,
                BirthDate = apply.BirthDate,
                Code = model.Code,
                FirstName = apply.FirstName,
                LastName = apply.LastName,
                Sex = apply.Sex,
                SpecialityIds = specialities.Select(s => s.SpecialityId).ToHashSet()
            };
            var studentEvent = await _studentService.AddAsync(apply.Space, studentAddModel, actor);
            var student = studentEvent.Item;

            apply.Student = student;
            apply.AcceptedAt = DateTime.UtcNow;
            _dbContext.Update(apply);
            await _dbContext.SaveChangesAsync();

            var publisherIds = new List<string>
            {
                apply.Space.PublisherId, 
                apply.User.PublisherId,
                apply.PublisherId,
                student.PublisherId
            };
            return await _eventService.EmitAsync(publisherIds, actor.ActorId, "APPLY_ACCEPT", new {});
        }
        
        
        public async Task<Event> RejectAsync(Apply apply, ApplyAcceptModel model, User actor)
        {
            AssertHelper.NotNull(apply, nameof(apply));
            AssertHelper.NotNull(apply.Space, nameof(apply.Space));
            AssertHelper.NotNull(actor, nameof(actor));
            AssertHelper.NotNull(model, nameof(model));

         
            apply.RejectedAt = DateTime.UtcNow;
            _dbContext.Update(apply);
            await _dbContext.SaveChangesAsync();

            var publisherIds = new List<string>
            {
                apply.Space.PublisherId, 
                apply.User.PublisherId,
                apply.PublisherId
            };
            return await _eventService.EmitAsync(publisherIds, actor.ActorId, "APPLY_REJECT", new {});
        }

        public async Task<bool> ContainsAsync(Space space, string userId)
        {
            AssertHelper.NotNull(space, nameof(space));

            return await _dbContext.Set<Apply>()
                .AnyAsync(s => space.Id == s.SpaceId && s.UserId == userId && s.DeletedAt == null);
        }
        
        
        
        

        public async Task<Apply?> FindByCodeAsync(Space space, string code)
        {
            AssertHelper.NotNull(space, nameof(space));
            AssertHelper.NotNullOrWhiteSpace(code, nameof(code));

            string normalized = code.Normalize().ToUpper();
            var apply = await _dbContext.Set<Apply>()
                .FirstOrDefaultAsync(s => space.Id == s.SpaceId && s.Code == normalized);

            if (apply == null)
            {
                throw new ElementNotFoundException("ApplyNotFoundByCode");
            }

            return apply;
        }

        
        
        public async Task<Event> DeleteAsync(Apply apply, User user)
        {
            AssertHelper.NotNull(apply, nameof(apply));
            AssertHelper.NotNull(apply.Space, nameof(apply.Space));
            AssertHelper.NotNull(user, nameof(user));
           var applySpecialities = await _dbContext.Set<ApplySpeciality>()
                .Where(p => apply.Id  == p.ApplyId)
                .ToListAsync();

           apply.FirstName = "";
           apply.LastName = "";
           apply.Sex = '0';
           apply.BirthDate = DateTime.MinValue;
           apply.Code = "";
           apply.NormalizedCode = "";
           apply.DeletedAt = DateTime.Now;

           _dbContext.RemoveRange(applySpecialities);
           _dbContext.Update(apply);
           await _dbContext.SaveChangesAsync();
           
           var publisherIds = new List<string> {
               apply.PublisherId, 
               apply.Space.PublisherId
           };

           return await _eventService.EmitAsync(publisherIds, user.ActorId, "APPLY_DELETE", apply);
        }
    }
}