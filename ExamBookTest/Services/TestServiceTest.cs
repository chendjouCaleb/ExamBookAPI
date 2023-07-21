using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ExamBook.Entities;
using ExamBook.Helpers;
using ExamBook.Identity.Entities;
using ExamBook.Identity.Services;
using ExamBook.Models;
using ExamBook.Persistence;
using ExamBook.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Traceability.Asserts;
using Traceability.Models;
using Traceability.Services;

#pragma warning disable NUnit2005
namespace ExamBookTest.Services
{
	public class TestServiceTest
	{
		private IServiceProvider _provider = null!;
		private TestService _testService = null!;
		private CourseService _courseService = null!;
		private CourseTeacherService _courseTeacherService = null!;
		private MemberService _memberService = null!;
		private StudentService _studentService = null!;
		private ExaminationService _examinationService = null!;
		private SpaceService _spaceService = null!;
		private RoomService _roomService = null!;
		private SpecialityService _specialityService = null!;
		private PublisherService _publisherService = null!;
		private EventAssertionsBuilder _eventAssertionsBuilder = null!;

		private ApplicationDbContext _dbContext = null!;
		private User _adminUser = null!;
		private Actor _actor = null!;

		private Space _space = null!;
		private Examination _examination = null!;
		private Speciality _speciality1;
		private Speciality _speciality2;
		private Speciality _speciality3;
		private Room _room = null!;
		private Course _course = null!;
		private TestAddModel _model = null!;


		[SetUp]
		public async Task Setup()
		{
			var services = new ServiceCollection();

			_provider = services.Setup();
			_examinationService = _provider.GetRequiredService<ExaminationService>();
			_publisherService = _provider.GetRequiredService<PublisherService>();
			_eventAssertionsBuilder = _provider.GetRequiredService<EventAssertionsBuilder>();
			_dbContext = _provider.GetRequiredService<ApplicationDbContext>();

			var userService = _provider.GetRequiredService<UserService>();
			_spaceService = _provider.GetRequiredService<SpaceService>();
			_memberService = _provider.GetRequiredService<MemberService>();
			_courseService = _provider.GetRequiredService<CourseService>();
			_courseTeacherService = _provider.GetRequiredService<CourseTeacherService>();
			_testService = _provider.GetRequiredService<TestService>();
			_studentService = _provider.GetRequiredService<StudentService>();
			_roomService = _provider.GetRequiredService<RoomService>();
			_specialityService = _provider.GetRequiredService<SpecialityService>();
			_adminUser = await userService.AddUserAsync(ServiceExtensions.UserAddModel);
			_actor = await userService.GetActor(_adminUser);

			var result = await _spaceService.AddAsync(_adminUser.Id, new SpaceAddModel
			{
				Name = "UY-1, PHILOSOPHY, L1",
				Identifier = "uy1_phi_l1"
			});
			_space = result.Item;

			var roomModel = new RoomAddModel {Capacity = 10, Name = "Room name"};
			_room = (await _roomService.AddRoomAsync(_space, roomModel, _adminUser)).Item;

			_speciality1 = (await _specialityService.AddSpecialityAsync(_space, "Speciality1", _adminUser)).Item;
			_speciality2 = (await _specialityService.AddSpecialityAsync(_space, "Speciality2", _adminUser)).Item;
			_speciality3 = (await _specialityService.AddSpecialityAsync(_space, "Speciality3", _adminUser)).Item;

			var courseResult = await _courseService.AddCourseAsync(_space, new CourseAddModel()
			{
				Code = "125",
				Coefficient = 10,
				Name = "Name",
				SpecialityIds = new HashSet<ulong>() { _speciality1.Id, _speciality2.Id }
			}, _adminUser);

			_course = courseResult.Item;
			_examination = (await _examinationService.AddAsync(_space, new ExaminationAddModel()
			{
				Name = "Exam name",
				SpecialitiyIds = new HashSet<ulong>() {_speciality1.Id, _speciality2.Id}
			}, _adminUser)).Item;
			
			_model = new TestAddModel
			{
				Name = "test name",
				Coefficient = 5,
				Duration = 60,
				StartAt = DateTime.UtcNow.AddMinutes(30),
				Radical = 30
			};
		}


		[Test]
		public async Task CreateTest()
		{
			var specialities = new List<Speciality> {_speciality1, _speciality2};

			var test = await _testService.CreateTestAsync(_space, _model, specialities);

			await _dbContext.Entry(test).ReloadAsync();

			Assert.AreEqual(_model.Name, test.Name);
			Assert.AreEqual(_model.Coefficient, test.Coefficient);
			Assert.AreEqual(_model.Radical, test.Radical);
			Assert.AreEqual(_model.StartAt, test.StartAt);
			Assert.AreEqual(_model.Duration, test.Duration);
			Assert.AreEqual(StringHelper.Normalize(_model.Name), test.NormalizedName);
			Assert.AreEqual(_space.Id, test.SpaceId);

			Assert.NotNull(test.Publisher);
			Assert.IsNotEmpty(test.PublisherId);

			Assert.AreEqual(2, test.TestSpecialities.Count);
			foreach (var speciality in specialities)
			{
				var testSpeciality = test.TestSpecialities.Find(ts => ts.SpecialityId == speciality.Id);
				Assert.NotNull(testSpeciality);
				Assert.AreEqual(test.Id, testSpeciality!.TestId);
				Assert.NotNull(testSpeciality.Publisher);
				Assert.IsNotEmpty(testSpeciality.PublisherId);
			}
		}


		[Test]
		public async Task AddTest()
		{
			var specialities = new List<Speciality> {_speciality1, _speciality2};
			var result = await _testService.AddAsync(_space, _model, specialities, _adminUser);
			var test = result.Item;

			await _dbContext.Entry(test).ReloadAsync();
			var testSpecialities = await _dbContext.TestSpecialities
				.Where(t => t.TestId == test.Id)
				.ToListAsync();

			Assert.AreEqual(_model.Name, test.Name);
			Assert.AreEqual(_model.Coefficient, test.Coefficient);
			Assert.AreEqual(_model.Radical, test.Radical);
			Assert.AreEqual(_model.StartAt, test.StartAt);
			Assert.AreEqual(_model.Duration, test.Duration);
			Assert.AreEqual(StringHelper.Normalize(_model.Name), test.NormalizedName);
			Assert.AreEqual(_space.Id, test.SpaceId);
			Assert.IsNotEmpty(test.PublisherId);


			var addEvent = result.Event;
			var assertions = _eventAssertionsBuilder.Build(addEvent);
			assertions.HasName("TEST_ADD");
			assertions.HasActor(_actor);
			await assertions.HasPublisherIdAsync(test.PublisherId);
			await assertions.HasPublisherIdAsync(_space.PublisherId);
			assertions.HasData(test);
			
			foreach (var speciality in specialities)
			{
				var testSpeciality = testSpecialities.Find(ts => ts.SpecialityId == speciality.Id);
				Assert.NotNull(testSpeciality);
				Assert.AreEqual(test.Id, testSpeciality!.TestId);
				Assert.IsNotEmpty(test.PublisherId);
				await assertions.HasPublisherIdAsync(testSpeciality.PublisherId);
				await assertions.HasPublisherIdAsync(speciality.PublisherId);
			}
		}
		
		
		[Test]
		public async Task AddCourseTest()
		{
			var specialities = new List<Speciality> {_speciality1, _speciality2};
			var result = await _testService.AddAsync(_space, _course, _model, _adminUser);
			var test = result.Item;

			await _dbContext.Entry(test).ReloadAsync();
			var testSpecialities = await _dbContext.TestSpecialities
				.Where(t => t.TestId == test.Id)
				.ToListAsync();

			Assert.AreEqual(_model.Name, test.Name);
			Assert.AreEqual(_model.Coefficient, test.Coefficient);
			Assert.AreEqual(_model.Radical, test.Radical);
			Assert.AreEqual(_model.StartAt, test.StartAt);
			Assert.AreEqual(_model.Duration, test.Duration);
			Assert.AreEqual(StringHelper.Normalize(_model.Name), test.NormalizedName);
			Assert.AreEqual(_space.Id, test.SpaceId);
			Assert.AreEqual(_course, test.Course);
			Assert.IsNotEmpty(test.PublisherId);


			var addEvent = result.Event;
			var assertions = _eventAssertionsBuilder.Build(addEvent);
			assertions.HasName("TEST_ADD");
			assertions.HasActor(_actor);
			await assertions.HasPublisherIdAsync(test.PublisherId);
			await assertions.HasPublisherIdAsync(_space.PublisherId);
			await assertions.HasPublisherIdAsync(_course.PublisherId);
			assertions.HasData(test);
			
			foreach (var speciality in specialities)
			{
				var testSpeciality = testSpecialities.Find(ts => ts.SpecialityId == speciality.Id);
				var courseSpeciality = await _dbContext.CourseSpecialities
					.Where(cs => cs.SpecialityId == speciality.Id && cs.CourseId == _course.Id)
					.FirstOrDefaultAsync();
				
				Assert.NotNull(testSpeciality);
				Assert.NotNull(courseSpeciality!);
				Assert.AreEqual(test.Id, testSpeciality!.TestId);
				Assert.AreEqual(courseSpeciality!.Id, testSpeciality.CourseSpeciality!.Id);
				Assert.IsNotEmpty(test.PublisherId);
				await assertions.HasPublisherIdAsync(testSpeciality.PublisherId);
				await assertions.HasPublisherIdAsync(speciality.PublisherId);
				await assertions.HasPublisherIdAsync(courseSpeciality.PublisherId);
			}
		}

		
		[Test]
		public async Task AddExaminationTest()
		{
			var specialities = new List<Speciality> {_speciality1, _speciality2};
			var examinationSpecialities = await _dbContext.ExaminationSpecialities
				.Include(es => es.Examination)
				.Where(es => es.ExaminationId == _examination.Id)
				.ToListAsync();
			
			var result = await _testService.AddAsync(_examination, _model, examinationSpecialities, _adminUser);
			var test = result.Item;

			await _dbContext.Entry(test).ReloadAsync();
			var testSpecialities = await _dbContext.TestSpecialities
				.Where(t => t.TestId == test.Id)
				.ToListAsync();

			Assert.AreEqual(_model.Name, test.Name);
			Assert.AreEqual(_model.Coefficient, test.Coefficient);
			Assert.AreEqual(_model.Radical, test.Radical);
			Assert.AreEqual(_model.StartAt, test.StartAt);
			Assert.AreEqual(_model.Duration, test.Duration);
			Assert.AreEqual(StringHelper.Normalize(_model.Name), test.NormalizedName);
			Assert.AreEqual(_space.Id, test.SpaceId);
			Assert.AreEqual(_examination.Id, test.ExaminationId);
			Assert.IsNotEmpty(test.PublisherId);


			var addEvent = result.Event;
			var assertions = _eventAssertionsBuilder.Build(addEvent);
			assertions.HasName("TEST_ADD");
			assertions.HasActor(_actor);
			await assertions.HasPublisherIdAsync(test.PublisherId);
			await assertions.HasPublisherIdAsync(_space.PublisherId);
			assertions.HasData(test);
			
			foreach (var examinationSpeciality in examinationSpecialities)
			{
				var testSpeciality = testSpecialities
					.Find(ts => ts.ExaminationSpecialityId == examinationSpeciality.Id);
				Assert.NotNull(testSpeciality);
				Assert.AreEqual(test.Id, testSpeciality!.TestId);
				Assert.AreEqual(examinationSpeciality.Id, testSpeciality.ExaminationSpecialityId);
				Assert.AreEqual(examinationSpeciality.SpecialityId, testSpeciality.SpecialityId);
				Assert.IsNotEmpty(test.PublisherId);
				await assertions.HasPublisherIdAsync(testSpeciality.PublisherId);
				await assertions.HasPublisherIdAsync(examinationSpeciality.PublisherId);
				await assertions.HasPublisherIdAsync(examinationSpeciality.Speciality!.PublisherId);
			}
		}

		// [Test]
		// public async Task ChangeTestName()
		// {
		//     var result = await _testService.AddAsync(_space, _model, _adminUser);
		//     var test = result.Item;
		//
		//     var newName = "new test name";
		//     var eventData = new ChangeValueData<string>(test.Name, newName);
		//     var changeEvent = await _testService.ChangeNameAsync(test, newName, _adminUser);
		//     
		//     await _dbContext.Entry(test).ReloadAsync();
		//     
		//     Assert.AreEqual(newName, test.Name);
		//     Assert.AreEqual(StringHelper.Normalize(newName), test.NormalizedName);
		//
		//     var publisher = await _publisherService.GetByIdAsync(test.PublisherId);
		//     var spacePublisher = await _publisherService.GetByIdAsync(_space.PublisherId);
		//
		//     Assert.NotNull(publisher);
		//     _eventAssertionsBuilder.Build(changeEvent)
		//         .HasName("TEST_CHANGE_NAME")
		//         .HasActor(_actor)
		//         .HasPublisher(publisher)
		//         .HasPublisher(spacePublisher)
		//         .HasData(eventData);
		// }
		//
		//
		// [Test]
		// public async Task ChangeTestRadical()
		// {
		//     var result = await _testService.AddAsync(_space, _model, _adminUser);
		//     var test = result.Item;
		//
		//     var newRadical = 90u;
		//     var eventData = new ChangeValueData<uint>(test.Radical, newRadical);
		//     var changeEvent = await _testService.ChangeRadicalAsync(test, newRadical, _adminUser);
		//     
		//     await _dbContext.Entry(test).ReloadAsync();
		//     
		//     Assert.AreEqual(newRadical, test.Radical);
		//
		//     var publisher = await _publisherService.GetByIdAsync(test.PublisherId);
		//     var spacePublisher = await _publisherService.GetByIdAsync(_space.PublisherId);
		//
		//     Assert.NotNull(publisher);
		//     _eventAssertionsBuilder.Build(changeEvent)
		//         .HasName("TEST_CHANGE_RADICAL")
		//         .HasActor(_actor)
		//         .HasPublisher(publisher)
		//         .HasPublisher(spacePublisher)
		//         .HasData(eventData);
		// }
		//
		//
		// [Test]
		// public async Task ChangeTestCoefficient()
		// {
		//     var result = await _testService.AddAsync(_space, _model, _adminUser);
		//     var test = result.Item;
		//
		//     var newCoefficient = 90u;
		//     var eventData = new ChangeValueData<uint>(test.Coefficient, newCoefficient);
		//     var changeEvent = await _testService.ChangeCoefficientAsync(test, newCoefficient, _adminUser);
		//     
		//     await _dbContext.Entry(test).ReloadAsync();
		//     
		//     Assert.AreEqual(newCoefficient, test.Coefficient);
		//
		//     var publisher = await _publisherService.GetByIdAsync(test.PublisherId);
		//     var spacePublisher = await _publisherService.GetByIdAsync(_space.PublisherId);
		//
		//     Assert.NotNull(publisher);
		//     _eventAssertionsBuilder.Build(changeEvent)
		//         .HasName("TEST_CHANGE_COEFFICIENT")
		//         .HasActor(_actor)
		//         .HasPublisher(publisher)
		//         .HasPublisher(spacePublisher)
		//         .HasData(eventData);
		// }
		//
		//
		// [Test]
		// public async Task ChangeTestDuration()
		// {
		//     var result = await _testService.AddAsync(_space, _model, _adminUser);
		//     var test = result.Item;
		//
		//     var newDuration = 90u;
		//     var eventData = new ChangeValueData<uint>(test.Duration, newDuration);
		//     var changeEvent = await _testService.ChangeDurationAsync(test, newDuration, _adminUser);
		//     
		//     await _dbContext.Entry(test).ReloadAsync();
		//     
		//     Assert.AreEqual(newDuration, test.Duration);
		//
		//     var publisher = await _publisherService.GetByIdAsync(test.PublisherId);
		//     var spacePublisher = await _publisherService.GetByIdAsync(_space.PublisherId);
		//
		//     Assert.NotNull(publisher);
		//     _eventAssertionsBuilder.Build(changeEvent)
		//         .HasName("TEST_CHANGE_DURATION")
		//         .HasActor(_actor)
		//         .HasPublisher(publisher)
		//         .HasPublisher(spacePublisher)
		//         .HasData(eventData);
		// }
		//
		//
		//
		// [Test]
		// public async Task ChangeTestStartAt()
		// {
		//     var result = await _testService.AddAsync(_space, _model, _adminUser);
		//     var test = result.Item;
		//
		//     var newDate = DateTime.Now.AddDays(5);
		//     var eventData = new ChangeValueData<DateTime>(test.StartAt, newDate);
		//     var changeEvent = await _testService.ChangeStartAtAsync(test, newDate, _adminUser);
		//     
		//     await _dbContext.Entry(test).ReloadAsync();
		//     
		//     Assert.AreEqual(newDate, test.StartAt);
		//
		//     var publisher = await _publisherService.GetByIdAsync(test.PublisherId);
		//     var spacePublisher = await _publisherService.GetByIdAsync(_space.PublisherId);
		//
		//     Assert.NotNull(publisher);
		//     _eventAssertionsBuilder.Build(changeEvent)
		//         .HasName("TEST_CHANGE_START_AT")
		//         .HasActor(_actor)
		//         .HasPublisher(publisher)
		//         .HasPublisher(spacePublisher)
		//         .HasData(eventData);
		// }
		//
		// [Test]
		// public async Task TryChangeTestStartAtBeforeNow_ShouldThrow()
		// {
		//     var test = (await _testService.AddAsync(_space, _model, _adminUser)).Item;
		//
		//     var newDate = DateTime.Now.AddDays(-5);
		//     var ex = Assert.ThrowsAsync<IllegalValueException>(async () =>
		//     {
		//         await _testService.ChangeStartAtAsync(test, newDate, _adminUser);
		//     });
		//     
		//     Assert.AreEqual("TestStartDateBeforeNow", ex!.Message);
		// }
		//
		//
		// [Test]
		// public async Task TestLock()
		// {
		//     var result = await _testService.AddAsync(_space, _model, _adminUser);
		//     var test = result.Item;
		//
		//     var changeEvent = await _testService.LockAsync(test, _adminUser);
		//     
		//     await _dbContext.Entry(test).ReloadAsync();
		//     
		//     Assert.AreEqual(true, test.IsLock);
		//
		//     var publisher = await _publisherService.GetByIdAsync(test.PublisherId);
		//     var spacePublisher = await _publisherService.GetByIdAsync(_space.PublisherId);
		//
		//     Assert.NotNull(publisher);
		//     _eventAssertionsBuilder.Build(changeEvent)
		//         .HasName("TEST_LOCK")
		//         .HasActor(_actor)
		//         .HasPublisher(publisher)
		//         .HasPublisher(spacePublisher)
		//         .HasData(new {});
		// }
		//
		//
		// [Test]
		// public async Task TryLock_LockedTest_ShouldThrow()
		// {
		//     var test = (await _testService.AddAsync(_space, _model, _adminUser)).Item;
		//     await _testService.LockAsync(test, _adminUser);
		//
		//     var ex = Assert.ThrowsAsync<IllegalStateException>(async () =>
		//     {
		//         await _testService.LockAsync(test, _adminUser);
		//     });
		//     Assert.AreEqual("TestIsLocked", ex!.Message);
		// }
		//
		//
		//
		// public async Task TestUnLock()
		// {
		//     var result = await _testService.AddAsync(_space, _model, _adminUser);
		//     var test = result.Item;
		//
		//     await _testService.LockAsync(test, _adminUser);
		//     var changeEvent = await _testService.UnLockAsync(test, _adminUser);
		//     
		//     await _dbContext.Entry(test).ReloadAsync();
		//     
		//     Assert.AreEqual(false, test.IsLock);
		//
		//     var publisher = await _publisherService.GetByIdAsync(test.PublisherId);
		//     var spacePublisher = await _publisherService.GetByIdAsync(_space.PublisherId);
		//
		//     Assert.NotNull(publisher);
		//     _eventAssertionsBuilder.Build(changeEvent)
		//         .HasName("TEST_UNLOCK")
		//         .HasActor(_actor)
		//         .HasPublisher(publisher)
		//         .HasPublisher(spacePublisher)
		//         .HasData(new {});
		// }
		//
		//
		// [Test]
		// public async Task TryUnLock_UnLockedTest_ShouldThrow()
		// {
		//     var test = (await _testService.AddAsync(_space, _model, _adminUser)).Item;
		//
		//     var ex = Assert.ThrowsAsync<IllegalStateException>(async () =>
		//     {
		//         await _testService.UnLockAsync(test, _adminUser);
		//     });
		//     Assert.AreEqual("TestIsNotLocked", ex!.Message);
		// }
		//
		//
		// // [Test]
		// // public async Task DeleteExamination()
		// // {
		// //     // var examination = (await _examinationService.AddAsync(_space, _model, _adminUser)).Item;
		// //     //
		// //     // var deleteEvent = await _examinationService.DeleteAsync(examination, _adminUser);
		// //     // await _dbContext.Entry(examination).ReloadAsync();
		// //     //
		// //     // Assert.AreEqual("", examination.Name);
		// //     // Assert.AreEqual("", examination.NormalizedName);
		// //     // Assert.NotNull(examination.DeletedAt);
		// //     // Assert.True(examination.IsDeleted);
		// //     //
		// //     // var publisher = await _publisherService.GetByIdAsync(examination.PublisherId);
		// //     // var spacePublisher = await _publisherService.GetByIdAsync(_space.PublisherId);
		// //     //
		// //     // _eventAssertionsBuilder.Build(deleteEvent)
		// //     //     .HasName("EXAMINATION_DELETE")
		// //     //     .HasActor(_actor)
		// //     //     .HasPublisher(publisher)
		// //     //     .HasPublisher(spacePublisher)
		// //     //     .HasData(examination);
		// // }
		//
		//
		//
		// // [Test]
		// // public async Task IsExamination_WithDeletedExamination_ShouldBeFalse()
		// // {
		// //     var examination = (await _examinationService.AddAsync(_space, _model, _adminUser)).Item;
		// //     await _examinationService.DeleteAsync(examination, _adminUser);
		// //     var isExamination = await _examinationService.ContainsAsync(_space, _model.Name);
		// //     Assert.False(isExamination);
		// // }
		//
		//
		//
		//
		// [Test]
		// public async Task GetTest()
		// {
		//     var test = (await _testService.AddAsync(_space, _model, _adminUser)).Item;
		//     var resultTest = await _testService.GetByIdAsync(test.Id);
		//     Assert.AreEqual(test.Id, resultTest.Id);
		// }
		//
		//
		// [Test]
		// public void GetNonExistingTest_ShouldThrow()
		// {
		//     var ex = Assert.ThrowsAsync<ElementNotFoundException>(async () =>
		//     {
		//         await _testService.GetByIdAsync(9000000000);
		//     });
		//     
		//     Assert.AreEqual("TestNotFoundById", ex!.Message);
		// }
	}
}