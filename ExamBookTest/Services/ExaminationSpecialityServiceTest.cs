using System;
using System.Threading.Tasks;
using ExamBook.Entities;
using ExamBook.Exceptions;
using ExamBook.Helpers;
using ExamBook.Identity.Entities;
using ExamBook.Identity.Services;
using ExamBook.Models;
using ExamBook.Models.Data;
using ExamBook.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Traceability.Asserts;
using Traceability.Models;
using Traceability.Services;

#pragma warning disable NUnit2005
namespace ExamBookTest.Services
{
	public class ExaminationSpecialityServiceTest
	{
		private IServiceProvider _provider = null!;
		private ExaminationSpecialityService _examinationSpecialityService = null!;
		private SpaceService _spaceService = null!;
		private SpecialityService _specialityService = null!;
		private PublisherService _publisherService = null!;
		private EventAssertionsBuilder _eventAssertionsBuilder = null!;

		private DbContext _dbContext = null!;
		private User _adminUser = null!;
		private Actor _actor = null!;

		private Space _space = null!;
		private Examination _examination = null!;
		private Speciality _speciality = null!;
		private ExaminationSpecialityAddModel _model = null!;


		[SetUp]
		public async Task Setup()
		{
			var services = new ServiceCollection();

			_provider = services.Setup();
			ExaminationService examinationService = _provider.GetRequiredService<ExaminationService>();
			_examinationSpecialityService = _provider.GetRequiredService<ExaminationSpecialityService>();
			_publisherService = _provider.GetRequiredService<PublisherService>();
			_eventAssertionsBuilder = _provider.GetRequiredService<EventAssertionsBuilder>();
			_dbContext = _provider.GetRequiredService<DbContext>();

			var userService = _provider.GetRequiredService<UserService>();
			_spaceService = _provider.GetRequiredService<SpaceService>();
			_specialityService = _provider.GetRequiredService<SpecialityService>();
			_adminUser = await userService.AddUserAsync(ServiceExtensions.UserAddModel);
			_actor = await userService.GetActor(_adminUser);

			var result = await _spaceService.AddAsync(_adminUser.Id, new SpaceAddModel
			{
				Name = "UY-1, PHILOSOPHY, L1",
				Identifier = "uy1_phi_l1"
			});
			_space = result.Item;


			var specialityModel = new SpecialityAddModel {Name = "speciality name"};
			_speciality = (await _specialityService.AddSpecialityAsync(_space, specialityModel, _adminUser)).Item;

			var examinationAddModel = new ExaminationAddModel
			{
				Name = "Examination name",
				StartAt = DateTime.Now.AddDays(-12)
			};

			_examination = (await examinationService.AddAsync(_space, examinationAddModel, _adminUser)).Item;

			_model = new ExaminationSpecialityAddModel()
			{
				Name = "speciality name"
			};
		}


		[Test]
		public async Task Add()
		{
			var result = await _examinationSpecialityService.AddAsync(_examination, _speciality, _adminUser);
			var examinationSpeciality = result.Item;

			await _dbContext.Entry(examinationSpeciality).ReloadAsync();

			Assert.AreEqual(_speciality.Name, examinationSpeciality.Name);
			Assert.AreEqual(StringHelper.Normalize(_speciality.Name), examinationSpeciality.NormalizedName);
			Assert.AreEqual(_speciality.Description, examinationSpeciality.Description);
			Assert.AreEqual(_examination.Id, examinationSpeciality.ExaminationId);
			Assert.AreEqual(_speciality.Id, examinationSpeciality.SpecialityId);

			var publisher = await _publisherService.GetByIdAsync(examinationSpeciality.PublisherId);
			var spacePublisher = await _publisherService.GetByIdAsync(_space.PublisherId);
			var specialityPublisher = await _publisherService.GetByIdAsync(_speciality.PublisherId);
			var examinationPublisher = await _publisherService.GetByIdAsync(_examination.PublisherId);
			var addEvent = result.Event;

			Assert.NotNull(publisher);
			_eventAssertionsBuilder.Build(addEvent)
				.HasName("EXAMINATION_SPECIALITY_ADD")
				.HasActor(_actor)
				.HasPublisher(publisher)
				.HasPublisher(spacePublisher)
				.HasPublisher(specialityPublisher)
				.HasPublisher(examinationPublisher)
				.HasData(examinationSpeciality);
		}


		[Test]
		public async Task TryAddWithUsedName_ShouldThrow()
		{
			await _examinationSpecialityService.AddAsync(_examination, _model, _adminUser);

			var ex = Assert.ThrowsAsync<UsedValueException>(async () =>
			{
				await _examinationSpecialityService.AddAsync(_examination, _model, _adminUser);
			});

			Assert.AreEqual("ExaminationSpecialityNameUsed", ex!.Message);
			Assert.AreEqual(_model.Name, ex.Params[0]);
		}

		[Test]
		public async Task TryAddWithUsedSpeciality_ShouldThrow()
		{
			await _examinationSpecialityService.AddAsync(_examination, _speciality, _adminUser);

			var ex = Assert.ThrowsAsync<UsedValueException>(async () =>
			{
				await _examinationSpecialityService.AddAsync(_examination, _speciality, _adminUser);
			});

			Assert.AreEqual("ExaminationSpecialityExists", ex!.Message);
			Assert.AreEqual(_speciality.Id, ex.Params[0]);
		}


		[Test]
		public async Task ChangeExaminationSpecialityName()
		{
			var result = await _examinationSpecialityService.AddAsync(_examination, _model, _adminUser);
			var examinationSpeciality = result.Item;

			var newName = "new examination speciality name";
			var eventData = new ChangeValueData<string>(examinationSpeciality.Name, newName);
			var changeEvent = await _examinationSpecialityService
				.ChangeNameAsync(examinationSpeciality, newName, _adminUser);

			await _dbContext.Entry(examinationSpeciality).ReloadAsync();

			Assert.AreEqual(newName, examinationSpeciality.Name);
			Assert.AreEqual(StringHelper.Normalize(newName), examinationSpeciality.NormalizedName);

			var publisher = await _publisherService.GetByIdAsync(examinationSpeciality.PublisherId);
			var spacePublisher = await _publisherService.GetByIdAsync(_space.PublisherId);

			Assert.NotNull(publisher);
			_eventAssertionsBuilder.Build(changeEvent)
				.HasName("EXAMINATION_SPECIALITY_CHANGE_NAME")
				.HasActor(_actor)
				.HasPublisher(publisher)
				.HasPublisher(spacePublisher)
				.HasData(eventData);
		}


		[Test]
		public async Task TryChangeExaminationSpecialityName_WithUsedName_ShouldThrow()
		{
			var examinationSpeciality =
				(await _examinationSpecialityService.AddAsync(_examination, _model, _adminUser)).Item;

			string newName = examinationSpeciality.Name;

			var ex = Assert.ThrowsAsync<UsedValueException>(async () =>
			{
				await _examinationSpecialityService.ChangeNameAsync(examinationSpeciality, newName, _adminUser);
			});

			Assert.AreEqual("ExaminationSpecialityNameUsed", ex!.Message);
			Assert.AreEqual(_model.Name, ex.Params[0]);
		}

		[Test]
		public async Task AttachSpeciality()
		{
			var result = await _examinationSpecialityService.AddAsync(_examination, _model, _adminUser);
			var examinationSpeciality = result.Item;

			var action = await _examinationSpecialityService.AttachSpecialityAsync(examinationSpeciality, _speciality,
					_adminUser);

			await _dbContext.Entry(examinationSpeciality).ReloadAsync();
			Assert.AreEqual(examinationSpeciality.SpecialityId, _speciality.Id);
		
			_eventAssertionsBuilder.Build(action)
				.HasName("EXAMINATION_SPECIALITY_ATTACH_SPECIALITY")
				.HasActor(_actor)
				// .HasPublisher(publisher)
				//.HasPublisher(spacePublisher)
				//.HasPublisher(specialityPublisher)
				//.HasPublisher(examinationPublisher)
				.HasData(new {SpecialityId = _speciality.Id});
		}
		
		
		[Test]
		public async Task TryAttach_AttachedExaminationSpeciality_ShouldThrow()
		{
			var result = await _examinationSpecialityService.AddAsync(_examination, _speciality, _adminUser);
			var examinationSpeciality = result.Item;

			

			var ex = Assert.ThrowsAsync<IllegalOperationException>(async () =>
			{
				await _examinationSpecialityService.AttachSpecialityAsync(examinationSpeciality, _speciality, _adminUser);
			});

			Assert.AreEqual("ExaminationSpecialityHasSpeciality", ex!.Message);
		}
		
		[Test]
		public async Task DetachSpeciality()
		{
			var result = await _examinationSpecialityService.AddAsync(_examination, _speciality, _adminUser);
			var examinationSpeciality = result.Item;

			var action = await _examinationSpecialityService.DetachSpecialityAsync(examinationSpeciality, _adminUser);

			await _dbContext.Entry(examinationSpeciality).ReloadAsync();
			Assert.Null(examinationSpeciality.SpecialityId);
			Assert.Null(examinationSpeciality.Speciality);
		
			_eventAssertionsBuilder.Build(action)
				.HasName("EXAMINATION_SPECIALITY_DETACH_SPECIALITY")
				.HasActor(_actor)
				// .HasPublisher(publisher)
				//.HasPublisher(spacePublisher)
				//.HasPublisher(specialityPublisher)
				//.HasPublisher(examinationPublisher)
				.HasData(new {SpecialityId = _speciality.Id});
		}
		
		[Test]
		public async Task TryDetach_DetachedExaminationSpeciality_ShouldThrow()
		{
			var result = await _examinationSpecialityService.AddAsync(_examination, _model, _adminUser);
			var examinationSpeciality = result.Item;

			var ex = Assert.ThrowsAsync<IllegalOperationException>(async () =>
			{
				await _examinationSpecialityService.DetachSpecialityAsync(examinationSpeciality, _adminUser);
			});

			Assert.AreEqual("ExaminationSpecialityHasNoSpeciality", ex!.Message);
		}



		[Test]
		public async Task ContainsExamination()
		{
			await _examinationSpecialityService.AddAsync(_examination, _model, _adminUser);
			var isExamination = await _examinationSpecialityService.ContainsAsync(_examination, _model.Name);
			Assert.True(isExamination);
		}


		[Test]
		public async Task Contains_WithNonExamination_ShouldBeFalse()
		{
			var isExamination = await _examinationSpecialityService
				.ContainsAsync(_examination, Guid.NewGuid().ToString());
			Assert.False(isExamination);
		}


		[Test]
		public async Task GetExaminationSpeciality()
		{
			var examinationSpeciality = (await _examinationSpecialityService.AddAsync(_examination, _model, _adminUser))
				.Item;

			var resultExamination = await _examinationSpecialityService.GetByIdAsync(examinationSpeciality.Id);
			Assert.AreEqual(examinationSpeciality.Id, resultExamination.Id);
		}


		[Test]
		public void GetNonExistingExaminationSpeciality_ShouldThrow()
		{
			var ex = Assert.ThrowsAsync<ElementNotFoundException>(async () =>
			{
				await _examinationSpecialityService.GetByIdAsync(9000000000);
			});

			Assert.AreEqual("ExaminationSpecialityNotFoundById", ex!.Message);
		}
	}
}