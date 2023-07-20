using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ExamBook.Entities;
using ExamBook.Exceptions;
using ExamBook.Helpers;
using ExamBook.Identity.Entities;
using ExamBook.Models;
using ExamBook.Models.Data;
using ExamBook.Persistence;
using ExamBook.Utils;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Traceability.Models;
using Traceability.Services;

namespace ExamBook.Services
{
	public class ExaminationSpecialityService
	{
		private readonly ApplicationDbContext _dbContext;
		private readonly PublisherService _publisherService;
		private readonly EventService _eventService;
		private readonly ILogger<ExaminationSpecialityService> _logger;

		public ExaminationSpecialityService(EventService eventService, 
			PublisherService publisherService, 
			ApplicationDbContext dbContext, 
			ILogger<ExaminationSpecialityService> logger)
		{
			_eventService = eventService;
			_publisherService = publisherService;
			_dbContext = dbContext;
			_logger = logger;
		}


		public async Task<ExaminationSpeciality> GetByIdAsync(ulong id)
		{
			var examinationSpeciality = await _dbContext.Set<ExaminationSpeciality>()
				.Include(e => e.Examination.Space)
				.Where(e => e.Id == id)
				.FirstOrDefaultAsync();

			if (examinationSpeciality == null)
			{
				throw new ElementNotFoundException("ExaminationSpecialityNotFoundById", id);
			}

			return examinationSpeciality;
		}
        
        
		public async Task<ExaminationSpeciality> GetByNameAsync(string name)
		{
			var normalizedName = StringHelper.Normalize(name);
			var examinationSpeciality = await _dbContext.Set<ExaminationSpeciality>()
				.Include(e => e.Examination.Space)
				.Where(e => e.NormalizedName == normalizedName)
				.FirstOrDefaultAsync();

			if (examinationSpeciality == null)
			{
				throw new ElementNotFoundException("ExaminationSpecialityNotFoundByName", name);
			}

			return examinationSpeciality;
		}
		
		public async Task<bool> ContainsAsync(Examination examination, string name)
		{
			var normalizedName = StringHelper.Normalize(name);
			return await _dbContext.Set<ExaminationSpeciality>()
				.AnyAsync(e => e.ExaminationId == examination.Id  && e.NormalizedName == normalizedName);
		}
		
		public async Task<bool> ContainsAsync(Examination examination, Speciality speciality)
		{
			AssertHelper.NotNull(examination, nameof(examination));
			AssertHelper.NotNull(speciality, nameof(speciality));
			return await _dbContext.Set<ExaminationSpeciality>()
				.AnyAsync(e => e.ExaminationId == examination.Id  && e.SpecialityId == speciality.Id);
		}


		public async Task<ActionResultModel<ExaminationSpeciality>> AddAsync(Examination examination,
			ExaminationSpecialityAddModel model, User actor)
		{
			AssertHelper.NotNull(examination.Space, nameof(examination.Space));
			AssertHelper.NotNull(actor, nameof(actor));
			AssertHelper.NotNull(model, nameof(model));

			if (await ContainsAsync(examination, model.Name))
			{
				throw new UsedValueException("ExaminationSpecialityNameUsed", model.Name);
			}

			var publisher = await _publisherService.AddAsync();
			ExaminationSpeciality examinationSpeciality = new ()
			{
				Examination = examination,
				Name = model.Name,
				NormalizedName = StringHelper.Normalize(model.Name),
				Description = model.Description,
				PublisherId = publisher.Id
			};
			await _dbContext.AddAsync(examinationSpeciality);
			await _dbContext.SaveChangesAsync();

			var publisherIds = new List<string> {examination.Space.PublisherId, examination.PublisherId, publisher.Id};
			var @event = await _eventService.EmitAsync(publisherIds, actor.ActorId, 
				"EXAMINATION_SPECIALITY_ADD", 
				examinationSpeciality);
			_logger.LogInformation("New examination service");
			return new ActionResultModel<ExaminationSpeciality>(examinationSpeciality, @event);
		}


		public async Task<ActionResultModel<ExaminationSpeciality>> AddAsync(Examination examination,
			Speciality speciality, User actor)
		{
			AssertHelper.NotNull(examination.Space, nameof(examination.Space));
			AssertHelper.NotNull(actor, nameof(actor));
			AssertHelper.NotNull(speciality, nameof(speciality));

			AssertHelper.IsTrue(examination.SpaceId == speciality.SpaceId);
			if (await ContainsAsync(examination, speciality))
			{
				throw new UsedValueException("ExaminationSpecialityExists", speciality.Id);
			}

			var publisher = await _publisherService.AddAsync();
			ExaminationSpeciality examinationSpeciality = new ()
			{
				Name = speciality.Name,
				NormalizedName = speciality.NormalizedName,
				Description = speciality.Description,
				Examination = examination,
				Speciality = speciality,
				PublisherId = publisher.Id
			};
			await _dbContext.AddAsync(examinationSpeciality);
			await _dbContext.SaveChangesAsync();

			var publisherIds = new List<string> {
				examination.Space.PublisherId, 
				examination.PublisherId, 
				speciality.PublisherId,
				publisher.Id
			};
			var @event = await _eventService.EmitAsync(publisherIds, actor.ActorId, 
				"EXAMINATION_SPECIALITY_ADD", 
				examinationSpeciality);
			_logger.LogInformation("New examination service");
			return new ActionResultModel<ExaminationSpeciality>(examinationSpeciality, @event);
		}
		
		
		
		public async Task<Event> AttachSpecialityAsync(
			ExaminationSpeciality examinationSpeciality,
			Speciality speciality, User actor)
		{
			AssertHelper.NotNull(examinationSpeciality.Examination.Space, nameof(examinationSpeciality.Examination.Space));
			AssertHelper.NotNull(actor, nameof(actor));
			AssertHelper.NotNull(speciality, nameof(speciality));

			if (examinationSpeciality.SpecialityId != null)
			{
				throw new IllegalOperationException("ExaminationSpecialityHasSpeciality");
			}

			var examination = examinationSpeciality.Examination;
			AssertHelper.IsTrue(examination.SpaceId == speciality.SpaceId);
			if (await ContainsAsync(examination, speciality))
			{
				throw new UsedValueException("ExaminationSpecialityExists", speciality.Id);
			}

			examinationSpeciality.Speciality = speciality;
			
			_dbContext.Update(examinationSpeciality);
			await _dbContext.SaveChangesAsync();

			var publisherIds = new List<string> {
				examination.Space.PublisherId, 
				examination.PublisherId, 
				speciality.PublisherId,
				examinationSpeciality.PublisherId
			};
			
			_logger.LogInformation("examination speciality attach sepciality [Name={}]", speciality.Name);
			return await _eventService.EmitAsync(publisherIds, actor.ActorId, 
				"EXAMINATION_SPECIALITY_ATTACH_SPECIALITY", new {SpecialityId = speciality.Id});
			
		}
		
		
		public async Task<Event> DetachSpecialityAsync(ExaminationSpeciality examinationSpeciality, User actor)
		{
			AssertHelper.NotNull(examinationSpeciality.Examination.Space, nameof(examinationSpeciality.Examination.Space));
			AssertHelper.NotNull(actor, nameof(actor));

			var examination = examinationSpeciality.Examination;
			
			if (examinationSpeciality.SpecialityId == null)
			{
				throw new IllegalOperationException("ExaminationSpecialityHasNoSpeciality");
			}

			var speciality = examinationSpeciality.Speciality;
			examinationSpeciality.Speciality = null;
			examinationSpeciality.SpecialityId = null;
			
			_dbContext.Update(examinationSpeciality);
			await _dbContext.SaveChangesAsync();

			var publisherIds = new List<string> {
				examination.Space.PublisherId, 
				examination.PublisherId, 
				speciality.PublisherId,
				examinationSpeciality.PublisherId
			};
			
			_logger.LogInformation("examination speciality detach speciality [Name={}]", speciality.Name);
			return await _eventService.EmitAsync(publisherIds, actor.ActorId, 
				"EXAMINATION_SPECIALITY_DETACH_SPECIALITY", new {SpecialityId = speciality.Id});
			
		}
		
		
		public async Task<Event> ChangeNameAsync(ExaminationSpeciality examinationSpeciality, string name, User user)
		{
			AssertHelper.NotNull(examinationSpeciality, nameof(examinationSpeciality));
			AssertHelper.NotNull(user, nameof(user));
			AssertHelper.NotNull(examinationSpeciality.Examination.Space, nameof(examinationSpeciality.Examination.Space));
            
			if (await ContainsAsync(examinationSpeciality.Examination, name))
			{
				throw new UsedValueException("ExaminationSpecialityNameUsed", name);
			}

			var data = new ChangeValueData<string>(examinationSpeciality.Name, name);
			examinationSpeciality.Name = name;
			examinationSpeciality.NormalizedName = StringHelper.Normalize(name);
			_dbContext.Update(examinationSpeciality);
			await _dbContext.SaveChangesAsync();
            
			var publisherIds = new[]
			{
				examinationSpeciality.PublisherId, 
				examinationSpeciality.Examination.PublisherId,
				examinationSpeciality.Examination.Space.PublisherId
			};
			return await _eventService.EmitAsync(publisherIds, user.ActorId, "EXAMINATION_SPECIALITY_CHANGE_NAME", data);
		}
        
        
        
		public async Task<Event> ChangeDescriptionAsync(ExaminationSpeciality examinationSpeciality, string description, User user)
		{
			AssertHelper.NotNull(examinationSpeciality, nameof(examinationSpeciality));
			AssertHelper.NotNull(examinationSpeciality.Examination.Space, nameof(examinationSpeciality.Examination.Space));

			var eventData = new ChangeValueData<string>(examinationSpeciality.Description, description);

			examinationSpeciality.Description = description;
			_dbContext.Update(examinationSpeciality);
			await _dbContext.SaveChangesAsync();
            
			var publisherIds = new List<string> {
				examinationSpeciality.PublisherId, 
				examinationSpeciality.Examination.PublisherId,
				examinationSpeciality.Examination.Space.PublisherId
			};
			return await _eventService.EmitAsync(publisherIds, user.ActorId, "EXAMINATION_SPECIALITY_CHANGE_DESCRIPTION", eventData);
		}


		public async Task<Event> DeleteAsync(ExaminationSpeciality examinationSpeciality, User actor)
		{
			throw new NotImplementedException();
		}
	}
}