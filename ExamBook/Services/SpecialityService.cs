using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ExamBook.Entities;
using ExamBook.Exceptions;
using ExamBook.Helpers;
using ExamBook.Identity.Entities;
using ExamBook.Identity.Models;
using ExamBook.Models;
using ExamBook.Models.Data;
using ExamBook.Utils;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Traceability.Models;
using Traceability.Services;

namespace ExamBook.Services
{
    public class SpecialityService
    {
        private readonly DbContext _dbContext;
        private readonly PublisherService _publisherService;
        private readonly EventService _eventService;
        private readonly ILogger<SpecialityService> _logger;
        

        public SpecialityService(DbContext dbContext,
            ILogger<SpecialityService> logger, 
            PublisherService publisherService, 
            EventService eventService)
        {
            _dbContext = dbContext;
            _logger = logger;
            _publisherService = publisherService;
            _eventService = eventService;
        }

        public async Task<Speciality> GetAsync(ulong id)
        {
            var speciality = await _dbContext.Set<Speciality>().Where(r => r.Id == id)
                .Include(r => r.Space)
                .FirstOrDefaultAsync();

            if (speciality == null)
            {
                throw new ElementNotFoundException("SpecialityNotFound", id);
            }

            return speciality;
        }
        
        
        public async Task<ICollection<Speciality>> ListAsync(HashSet<ulong> specialityIds)
        {
            var specialities = await _dbContext.Set<Speciality>()
                .Where(s => specialityIds.Contains(s.Id))
                .Include(r => r.Space)
                .ToListAsync();

            var notFounds = specialityIds.TakeWhile(id => specialities.All(s => s.Id != id));
            if (!notFounds.Any())
            {
                throw new ElementNotFoundException("SpecialityNotFoundByIds", notFounds);
            }

            return specialities;
        }

        public async Task<ActionResultModel<Speciality>> AddSpecialityAsync(Space space,string name, User user)
        {
            var model = new SpecialityAddModel { Name = name };
            return await AddSpecialityAsync(space, model, user);
        }

        public async Task<ActionResultModel<Speciality>> AddSpecialityAsync(Space space, SpecialityAddModel model, User user)
        {
            AssertHelper.NotNull(space, nameof(space));
            AssertHelper.NotNull(model, nameof(space));
            AssertHelper.NotNull(user, nameof(user));

            if (await ContainsAsync(space, model.Name))
            {
                throw new UsedValueException("SpecialityNameUsed");
            }

            var publisher = await _publisherService.AddAsync();
            var normalizedName = StringHelper.Normalize(model.Name);
            Speciality speciality = new()
            {
                Space = space,
                Name = model.Name,
                NormalizedName = normalizedName,
                Description = model.Description,
                PublisherId = publisher.Id
            };

            await _dbContext.AddAsync(speciality);
            await _dbContext.SaveChangesAsync();
            var publisherIds = new[] {publisher.Id, space.PublisherId};
            var @event = await _eventService.EmitAsync(publisherIds, user.ActorId, "SPECIALITY_ADD", speciality);
            _logger.LogInformation("New speciality created: {}", speciality.Name);
            return new ActionResultModel<Speciality>(speciality, @event);
        }
        
        
        public async Task<Event> ChangeNameAsync(Speciality speciality, string name, User user)
        {
            AssertHelper.NotNull(speciality, nameof(speciality));
            AssertHelper.NotNull(user, nameof(user));
            AssertHelper.NotNull(speciality.Space, nameof(speciality.Space));
            
            if (await ContainsAsync(speciality.Space!, name))
            {
                throw new UsedValueException("SpecialityNameUsed");
            }

            var data = new ChangeValueData<string>(speciality.Name, name);
            speciality.Name = name;
            speciality.NormalizedName = StringHelper.Normalize(name);
            _dbContext.Update(speciality);
            await _dbContext.SaveChangesAsync();
            
            var publisherIds = new[] {speciality.PublisherId, speciality.Space!.PublisherId};
            return await _eventService.EmitAsync(publisherIds, user.ActorId, "SPECIALITY_CHANGE_NAME", data);
        }
        
        
        
        public async Task<Event> ChangeDescriptionAsync(Speciality speciality, string description, User user)
        {
            AssertHelper.NotNull(speciality, nameof(speciality));
            AssertHelper.NotNull(speciality.Space, nameof(speciality.Space));

            var eventData = new ChangeValueData<string>(speciality.Description, description);

            speciality.Description = description;
            _dbContext.Update(speciality);
            await _dbContext.SaveChangesAsync();
            
            var publisherIds = new List<string> {
                speciality.PublisherId, 
                speciality.Space!.PublisherId
            };
            return await _eventService.EmitAsync(publisherIds, user.ActorId, "SPECIALITY_CHANGE_DESCRIPTION", eventData);
        }


        public async Task<bool> ContainsAsync(Space space, string name)
        {
            AssertHelper.NotNull(space, nameof(space));
            AssertHelper.NotNullOrWhiteSpace(name, nameof(name));
            var normalized = name.Normalize().ToUpper();
            return await  _dbContext.Set<Speciality>()
                .AnyAsync(s => space.Equals(s.Space) && s.NormalizedName == normalized);
        }
        
        public async Task<Speciality> GetByNameAsync(Space space, string name)
        {
            string normalizedName = StringHelper.Normalize(name);
            var speciality = await _dbContext.Set<Speciality>()
                .Where(r => r.NormalizedName == normalizedName && space.Id == r.SpaceId)
                .FirstOrDefaultAsync();

            if (speciality == null)
            {
                throw new ElementNotFoundException("SpecialityNotFoundByName");
            }

            return speciality;
        }


        public async Task<Event> DeleteAsync(Speciality speciality, User user)
        {
            AssertHelper.NotNull(speciality, nameof(speciality));
            AssertHelper.NotNull(speciality.Space, nameof(speciality.Space));
            AssertHelper.NotNull(user, nameof(user));

            speciality.Name = "";
            speciality.NormalizedName = "";
            speciality.DeletedAt = DateTime.UtcNow;

            _dbContext.Update(speciality);
            await _dbContext.SaveChangesAsync();
            
            var publisherIds = new[] {speciality.PublisherId, speciality.Space!.PublisherId};
            
            return await _eventService.EmitAsync(publisherIds, user.ActorId, "SPECIALITY_DELETE", speciality);
        }

        public async Task DestroyAsync(Speciality speciality)
        {
            AssertHelper.NotNull(speciality, nameof(speciality));
            _dbContext.Remove(speciality);
            await _dbContext.SaveChangesAsync();
        }
    }
}