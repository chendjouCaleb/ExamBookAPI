using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ExamBook.Entities;
using ExamBook.Exceptions;
using ExamBook.Identity.Entities;
using ExamBook.Models;
using ExamBook.Persistence;
using ExamBook.Utils;
using Microsoft.EntityFrameworkCore;
using Traceability.Services;

namespace ExamBook.Services
{
	public class TestSpecialityService
	{
		private readonly ApplicationDbContext _dbContext;
		private readonly EventService _eventService;
		private readonly PublisherService _publisherService;


		public TestSpecialityService(ApplicationDbContext dbContext, EventService eventService, PublisherService publisherService)
		{
			_dbContext = dbContext;
			_eventService = eventService;
			_publisherService = publisherService;
		}

		public async Task<TestSpeciality> GetAsync(ulong id)
		{
			var testSpeciality = await _dbContext.TestSpecialities
				.Where(ts => ts.Id == id)
				.Include(ts => ts.Test)
				.Include(ts => ts.ExaminationSpeciality)
				.Include(ts => ts.Speciality)
				.FirstOrDefaultAsync();

			if (testSpeciality != null)
			{
				throw new ElementNotFoundException("TestSpecialityById", id);
			}

			return testSpeciality;
		}


		public async Task<bool> ContainsAsync(Test test, ExaminationSpeciality examinationSpeciality)
		{
			AssertHelper.NotNull(test, nameof(test));
			AssertHelper.NotNull(examinationSpeciality, nameof(examinationSpeciality));
			return await _dbContext.TestSpecialities
				.Where(ts => ts.TestId == test.Id && ts.ExaminationSpecialityId == examinationSpeciality.Id)
				.AnyAsync();
		}
		
		
		public async Task<bool> ContainsAsync(Test test, Speciality speciality)
		{
			AssertHelper.NotNull(test, nameof(test));
			AssertHelper.NotNull(speciality, nameof(speciality));
			return await _dbContext.TestSpecialities
				.Where(ts => ts.TestId == test.Id && ts.SpecialityId == speciality.Id)
				.AnyAsync();
		}

		public async Task<ActionResultModel<List<TestSpeciality>>> AddAsync
			(Test test, List<ExaminationSpeciality> specialities, User actor) 
		{
			AssertHelper.NotNull(test, nameof(test));
			AssertHelper.NotNull(test.Examination, nameof(test.Examination));
			AssertHelper.NotNull(test.Space, nameof(test.Space));
			AssertHelper.NotNull(actor, nameof(actor));
			AssertHelper.NotNull(specialities, nameof(specialities));
			AssertHelper.IsTrue(specialities.TrueForAll(es => es.Speciality != null));


			if (test.ExaminationId != null)
			{
				throw new InvalidStateException("TestShouldHaveExamination", test);
			}
			
			AssertHelper.IsTrue(specialities.TrueForAll(es => es.ExaminationId == test.ExaminationId));


			var testSpecialities = new List<TestSpeciality>();
			foreach (var examinationSpeciality in specialities)
			{
				var testSpeciality = await CreateSpeciality(test, examinationSpeciality);
				testSpecialities.Add(testSpeciality);
			}
			var testPublisherIds = testSpecialities.Select(t => t.PublisherId).ToList();
			
			await _dbContext.TestSpecialities.AddRangeAsync(testSpecialities);
			await _dbContext.SaveChangesAsync();
			await _publisherService.SaveAllAsync(testSpecialities.Select(ts => ts.Publisher!).ToList());

			var publisherIds = new List<string>
			{
				test.Space.PublisherId, 
				test.Examination!.PublisherId,
				test.PublisherId,
			};
			
			var specialityPublisherIds = specialities.Select(s => s.Speciality).Select(s => s!.PublisherId);
			var examinationSpecialitiesPublisherIds = specialities.Select(s => s.PublisherId);
			publisherIds.AddRange(specialityPublisherIds);
			publisherIds.AddRange(examinationSpecialitiesPublisherIds);
			publisherIds.AddRange(testPublisherIds);

			var data = testSpecialities.Select(ts => ts.Id).ToList();
			var action = await _eventService.EmitAsync(publisherIds, actor.ActorId, "TEST_SPECIALITIES_ADD", data);

			return new ActionResultModel<List<TestSpeciality>>(testSpecialities, action);
		}
		
		
		
		public async Task<ActionResultModel<List<TestSpeciality>>> AddAsync
			(Test test, List<Speciality> specialities, User actor) 
		{
			AssertHelper.NotNull(test, nameof(test));
			AssertHelper.NotNull(test.Examination, nameof(test.Examination));
			AssertHelper.NotNull(test.Space, nameof(test.Space));
			AssertHelper.NotNull(actor, nameof(actor));
			AssertHelper.NotNull(specialities, nameof(specialities));
			AssertHelper.IsTrue(specialities.TrueForAll(s => s.SpaceId == test.SpaceId));


			if (test.ExaminationId != null)
			{
				throw new InvalidStateException("TestShouldHaveExamination", test);
			}
			
			AssertHelper.IsTrue(specialities.TrueForAll(es => es.SpaceId == test.SpaceId));


			var testSpecialities = new List<TestSpeciality>();
			foreach (var speciality in specialities)
			{
				var testSpeciality = await CreateSpeciality(test, speciality);
				testSpecialities.Add(testSpeciality);
			}
			var testPublisherIds = testSpecialities.Select(t => t.PublisherId).ToList();
			
			await _dbContext.TestSpecialities.AddRangeAsync(testSpecialities);
			await _dbContext.SaveChangesAsync();
			await _publisherService.SaveAllAsync(testSpecialities.Select(ts => ts.Publisher!).ToList());

			var publisherIds = new List<string>
			{
				test.Space.PublisherId, 
				test.Examination!.PublisherId,
				test.PublisherId
			};
			
			var specialityPublisherIds = specialities.Select(s => s.PublisherId);
			publisherIds.AddRange(specialityPublisherIds);
			publisherIds.AddRange(testPublisherIds);

			var data = testSpecialities.Select(ts => ts.Id).ToList();
			var action = await _eventService.EmitAsync(publisherIds, actor.ActorId, "TEST_SPECIALITIES_ADD", data);

			return new ActionResultModel<List<TestSpeciality>>(testSpecialities, action);
		}


		public async Task<TestSpeciality> CreateSpeciality(Test test, ExaminationSpeciality examinationSpeciality)
		{
			AssertHelper.NotNull(test, nameof(test));
			AssertHelper.NotNull(examinationSpeciality, nameof(examinationSpeciality));
			AssertHelper.NotNull(examinationSpeciality.Speciality, nameof(examinationSpeciality.Speciality));
			
			if (test.ExaminationId == null)
			{
				throw new InvalidStateException("TestShouldHaveExamination", test);
			}

			if (await ContainsAsync(test, examinationSpeciality))
			{
				throw new DuplicateValueException("TestExaminationSpecialityExists", test, examinationSpeciality);
			}

			var publisher = _publisherService.Create();

			return new TestSpeciality
			{
				Test = test,
				ExaminationSpeciality = examinationSpeciality,
				Speciality = examinationSpeciality.Speciality,
				Publisher = publisher,
				PublisherId = publisher.Id
			};
		}
		
		
		public async Task<TestSpeciality> CreateSpeciality(Test test, Speciality speciality)
		{
			AssertHelper.NotNull(test, nameof(test));
			AssertHelper.NotNull(speciality, nameof(speciality));
			AssertHelper.IsTrue(test.SpaceId == speciality.SpaceId);
			
			if (test.ExaminationId != null)
			{
				throw new InvalidStateException("TestShouldNotHaveExamination", test);
			}

			if (await ContainsAsync(test, speciality))
			{
				throw new DuplicateValueException("TestSpecialityExists", test, speciality);
			}

			var publisher = _publisherService.Create();

			return new TestSpeciality
			{
				Test = test,
				Speciality = speciality,
				SpecialityId = speciality.Id,
				Publisher = publisher,
				PublisherId = publisher.Id
			};
		}
	}
}