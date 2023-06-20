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
using Vx.Models;
using Vx.Services;

namespace ExamBook.Services
{
    public class ExaminationService
    {
        private readonly DbContext _dbContext;
        private readonly PublisherService _publisherService;
        private readonly EventService _eventService;
        private readonly ILogger<ExaminationService> _logger;
        

        public ExaminationService(DbContext dbContext, 
            PublisherService publisherService, 
            EventService eventService, 
            ILogger<ExaminationService> logger)
        {
            _dbContext = dbContext;
            _publisherService = publisherService;
            _eventService = eventService;
            _logger = logger;
        }


        public async Task<Examination> GetByIdAsync(ulong id)
        {
            var examination = await _dbContext.Set<Examination>()
                .Include(e => e.Space)
                .Where(e => e.Id == id)
                .FirstOrDefaultAsync();

            if (examination == null)
            {
                throw new ElementNotFoundException("ExaminationNotFoundById{0}", id);
            }

            return examination;
        }
        
        
        public async Task<Examination> GetByNameAsync(string name)
        {
            var normalizedName = StringHelper.Normalize(name);
            var examination = await _dbContext.Set<Examination>()
                .Include(e => e.Space)
                .Where(e => e.NormalizedName == normalizedName)
                .FirstOrDefaultAsync();

            if (examination == null)
            {
                throw new ElementNotFoundException("ExaminationNotFoundByName{0}", name);
            }

            return examination;
        }

        public async Task<ActionResultModel<Examination>> AddAsync(Space space, ExaminationAddModel model, User user)
        {
            AssertHelper.NotNull(space, nameof(space));
            AssertHelper.NotNull(model, nameof(model));
            AssertHelper.NotNull(user, nameof(user));
            
            if (await ContainsAsync(space, model.Name))
            {
                throw new UsedValueException("ExaminationNameUsed{0}", model.Name);
            }

            if (model.StartAt > DateTime.Now)
            {
                throw new IllegalValueException("StartDateBeforeNow");
            }

            var specialities = await _dbContext.Set<Speciality>()
                .Where(s => model.SpecialitiyIds.Contains(s.Id))
                .ToListAsync();
            
            var publisher = await _publisherService.AddAsync();
            Examination examination = new ()
            {
                Space = space,
                SpaceId = space.Id,
                Name = model.Name,
                NormalizedName = StringHelper.Normalize(model.Name),
                StartAt = model.StartAt,
                PublisherId = publisher.Id
            };
            examination.ExaminationSpecialities = (await CreateSpecialities(examination, specialities)).ToList();
            await _dbContext.AddAsync(examination);
            await _dbContext.SaveChangesAsync();

            var publisherIds = new List<string> {space.PublisherId, publisher.Id};
            publisherIds.AddRange(specialities.Select(s => s.PublisherId));
            var @event = await _eventService.EmitAsync(publisherIds, user.ActorId, "EXAMINATION_ADD", examination);
            _logger.LogInformation("New examination service");
            return new ActionResultModel<Examination>(examination, @event);
        }


        

        public async Task<bool> ContainsAsync(Space space, string name)
        {
            var normalizedName = StringHelper.Normalize(name);
            return await _dbContext.Set<Examination>()
                .AnyAsync(e => e.SpaceId == space.Id  && e.NormalizedName == normalizedName);
        }
        

        public async Task<Event> ChangeNameAsync(Examination examination, string name, User user)
        {
            AssertHelper.NotNull(examination, nameof(examination));
            AssertHelper.NotNull(examination.Space, nameof(examination.Space));
            AssertHelper.NotNullOrWhiteSpace(name, nameof(name));
            
            if (await ContainsAsync(examination.Space, name))
            {
                throw new UsedValueException("ExaminationNameUsed{0}", name);
            }

            string normalizedName = StringHelper.Normalize(name);
            var eventData = new ChangeValueData<string>(examination.Name, name);
            examination.Name = name;
            examination.NormalizedName = normalizedName;
            _dbContext.Update(examination);
            await _dbContext.SaveChangesAsync();
            var publisherIds = new List<string> { examination.PublisherId, examination.Space.PublisherId };
            return await _eventService.EmitAsync(publisherIds, user.ActorId, "EXAMINATION_CHANGE_NAME", eventData);
        }


        public async Task<Event> ChangeStartAtAsync(Examination examination, DateTime startAt, User user)
        {
            AssertHelper.NotNull(examination, nameof(examination));
            AssertHelper.NotNull(examination.Space, nameof(examination.Space));
            
            if (startAt > DateTime.Now)
            {
                throw new IllegalValueException("StartDateBeforeNow");
            }

            var eventData = new ChangeValueData<DateTime>(examination.StartAt, startAt);
            examination.StartAt = startAt;

            _dbContext.Update(examination);
            await _dbContext.SaveChangesAsync();

            var publisherIds = new List<string> { examination.Space.PublisherId, examination.PublisherId };
            return await _eventService.EmitAsync(publisherIds, user.ActorId, "EXAMINATION_CHANGE_START_AT", eventData);
        }
        
        
        public async Task<Event> LockAsync(Examination examination, User user)
        {
            AssertHelper.NotNull(examination, nameof(examination));
            AssertHelper.NotNull(examination.Space, nameof(examination.Space));
            
            if (examination.IsLock)
            {
                throw new IllegalStateException("ExaminationIsLocked");
            }

            examination.IsLock = true;
            _dbContext.Update(examination);
            await _dbContext.SaveChangesAsync();

            var publisherIds = new List<string> { examination.Space.PublisherId, examination.PublisherId };
            return await _eventService.EmitAsync(publisherIds, user.ActorId, "EXAMINATION_LOCK", new {});
        }
        
        
        public async Task<Event> UnLockAsync(Examination examination, User user)
        {
            AssertHelper.NotNull(examination, nameof(examination));
            AssertHelper.NotNull(examination.Space, nameof(examination.Space));
            
            if (!examination.IsLock)
            {
                throw new IllegalStateException("ExaminationIsNotLocked");
            }

            examination.IsLock = false;
            _dbContext.Update(examination);
            await _dbContext.SaveChangesAsync();

            var publisherIds = new List<string> { examination.Space.PublisherId, examination.PublisherId };
            return await _eventService.EmitAsync(publisherIds, user.ActorId, "EXAMINATION_UNLOCK", new {});
        }
        
        
        public async Task<ActionResultModel<ExaminationSpeciality>> AddSpeciality(Examination examination, 
            Speciality speciality, User user)
        {
            AssertHelper.NotNull(user, nameof(user));

            var examinationSpeciality = await CreateSpeciality(examination, speciality);

            var publisherIds = new List<string>
            {
                examination.Space.PublisherId, 
                examination.PublisherId, 
                speciality.PublisherId
            };

            await _dbContext.AddAsync(examinationSpeciality);
            await _dbContext.SaveChangesAsync();
            var @event = await _eventService.EmitAsync(publisherIds, user.ActorId, "EXAMINATION_SPECIALITY_ADD",
                examinationSpeciality);
            
            return new ActionResultModel<ExaminationSpeciality>(examinationSpeciality, @event);
        }
        
        
        
        public async Task<ActionResultModel<ICollection<ExaminationSpeciality>>> AddSpecialities(Examination examination, 
            ICollection<Speciality> specialities, User user)
        {
            
            AssertHelper.NotNull(specialities, nameof(specialities));
            AssertHelper.NotNull(user, nameof(user));
            var examinationSpecialities = await CreateSpecialities(examination, specialities);

            await _dbContext.AddRangeAsync(examinationSpecialities);
            await _dbContext.SaveChangesAsync();
            
            var publisherIds = new List<string> {examination.Space.PublisherId, examination.PublisherId};
            publisherIds.AddRange(specialities.Select(s => s.PublisherId));
            
            var @event = await _eventService.EmitAsync(publisherIds, user.ActorId, "EXAMINATION_SPECIALITIES_ADD",
                examinationSpecialities);
            
            return new ActionResultModel<ICollection<ExaminationSpeciality>>(examinationSpecialities, @event);
        }
        
        
        public async Task<ICollection<ExaminationSpeciality>> CreateSpecialities(Examination examination, 
            ICollection<Speciality> specialities)
        {
            
            AssertHelper.NotNull(specialities, nameof(specialities));
            var examinationSpecialities = new List<ExaminationSpeciality>();
            
            foreach (var speciality in specialities)
            {
                examinationSpecialities.Add(await CreateSpeciality(examination,speciality));
            }

            return examinationSpecialities;
        }
        
        
        public async Task<ExaminationSpeciality> CreateSpeciality(Examination examination, Speciality speciality)
        {
            AssertHelper.NotNull(examination, nameof(examination));
            AssertHelper.NotNull(examination.Space, nameof(examination.Space));
            AssertHelper.NotNull(speciality, nameof(speciality));

            if (examination.SpaceId != speciality.SpaceId)
            {
                throw new IncompatibleEntityException(examination, speciality);
            }

            if (await ContainsSpecialityAsync(examination, speciality))
            {
                throw new IllegalOperationException("ExaminationSpecialityAlreadyExists");
            }

            ExaminationSpeciality examinationSpeciality = new ()
            {
                Examination = examination,
                Speciality = speciality
            };

            return examinationSpeciality;
        }
        
        
        public async Task<Examination> GetSpecialityAsync(Examination examination, Speciality speciality)
        {
            AssertHelper.NotNull(examination, nameof(examination));
            AssertHelper.NotNull(speciality, nameof(speciality));
          
            var examinationSpeciality = await _dbContext.Set<ExaminationSpeciality>()
                .FirstOrDefaultAsync(e => e.ExaminationId == examination.Id 
                                          && e.SpecialityId == speciality.Id);


            if (examinationSpeciality == null)
            {
                throw new ElementNotFoundException("ExaminationSpecialityNotFound");
            }

            return examination;
        }
        
        public async Task<bool> ContainsSpecialityAsync(Examination examination, Speciality speciality)
        {
            AssertHelper.NotNull(examination, nameof(examination));
            AssertHelper.NotNull(speciality, nameof(speciality));
            
            return await _dbContext.Set<ExaminationSpeciality>()
                .AnyAsync(e => e.ExaminationId == examination.Id 
                               && e.SpecialityId == speciality.Id);
        }
        
        public async Task DeleteSpecialityAsync(ExaminationSpeciality examinationSpeciality)
        {
            AssertHelper.NotNull(examinationSpeciality, nameof(examinationSpeciality));
            _dbContext.Remove(examinationSpeciality);
            await _dbContext.SaveChangesAsync();
        }

        public async Task<Event> DeleteAsync(Examination examination, User user)
        {
            AssertHelper.NotNull(user, nameof(user));
            AssertHelper.NotNull(examination, nameof(examination));
            AssertHelper.NotNull(examination.Space, nameof(examination.Space));
            
            var specialities = _dbContext.Set<ExaminationSpeciality>()
                .Where(e => e.ExaminationId == examination.Id);
            
            _dbContext.RemoveRange(specialities);
            _dbContext.Remove(examination);
            await _dbContext.SaveChangesAsync();

            var publisherIds = new List<string> {examination.Space.PublisherId, examination.PublisherId};
            var @event = await _eventService.EmitAsync(publisherIds, user.Id, "EXAMINATION_DELETE", examination);
            return @event;
        }
    }
}