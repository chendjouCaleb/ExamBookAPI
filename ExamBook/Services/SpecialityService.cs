using System;
using System.Linq;
using System.Threading.Tasks;
using ExamBook.Entities;
using ExamBook.Exceptions;
using ExamBook.Helpers;
using ExamBook.Identity.Models;
using ExamBook.Models;
using ExamBook.Models.Data;
using ExamBook.Utils;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Vx.Models;
using Vx.Services;

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

        public async Task<Speciality> GetSpecialityAsync(ulong id)
        {
            var speciality = await _dbContext.Set<Speciality>().Where(r => r.Id == id)
                .Include(r => r.Space)
                .FirstOrDefaultAsync();

            if (speciality == null)
            {
                throw new ElementNotFoundException("SpecialityNotFound");
            }

            return speciality;
        }

        public async Task<ActionResultModel<Speciality>> AddSpecialityAsync(Space space, SpecialityAddModel model, User user)
        {
            Asserts.NotNull(space, nameof(space));
            Asserts.NotNull(model, nameof(space));
            Asserts.NotNull(user, nameof(user));

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
            Asserts.NotNull(speciality, nameof(speciality));
            Asserts.NotNull(user, nameof(user));
            Asserts.NotNull(speciality.Space, nameof(speciality.Space));
            
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


        public async Task<bool> ContainsAsync(Space space, string name)
        {
            Asserts.NotNull(space, nameof(space));
            Asserts.NotNullOrWhiteSpace(name, nameof(name));
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
            Asserts.NotNull(speciality, nameof(speciality));
            Asserts.NotNull(speciality.Space, nameof(speciality.Space));
            Asserts.NotNull(user, nameof(user));

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
            Asserts.NotNull(speciality, nameof(speciality));
            var classroomSpecialities = _dbContext.Set<ClassroomSpeciality>()
                .Where(cs => speciality.Equals(cs.Speciality));

            _dbContext.RemoveRange(classroomSpecialities);
            _dbContext.Remove(speciality);
            await _dbContext.SaveChangesAsync();
        }
    }
}