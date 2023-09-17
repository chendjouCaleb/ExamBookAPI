using System;
using System.Threading.Tasks;
using ExamBook.Entities;
using ExamBook.Exceptions;
using ExamBook.Identity.Entities;
using ExamBook.Identity.Services;
using ExamBook.Models;
using ExamBook.Models.Data;
using ExamBook.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Social.Helpers;
using Traceability.Asserts;

#pragma warning disable NUnit2005
namespace ExamBookTest.Services
{
	public class CourseServiceTest
	{
		private IServiceProvider _provider = null!;
		private CourseService _service = null!;
		private SpaceService _spaceService = null!;
		private MemberService _memberService = null!;
		private EventAssertionsBuilder _eventAssertionsBuilder = null!;

		private DbContext _dbContext = null!;
		private User _adminUser = null!;
		private User _user1 = null!;
		
		private Member _adminMember = null!;

		private Space _space = null!;

		private CourseAddModel _model = null!;


		[SetUp]
		public async Task Setup()
		{
			var services = new ServiceCollection();

			_provider = services.Setup();
			_eventAssertionsBuilder = _provider.GetRequiredService<EventAssertionsBuilder>();
			_dbContext = _provider.GetRequiredService<DbContext>();

			var userService = _provider.GetRequiredService<UserService>();
			_spaceService = _provider.GetRequiredService<SpaceService>();
			_memberService = _provider.GetRequiredService<MemberService>();
			_service = _provider.GetRequiredService<CourseService>();
			_adminUser = await userService.AddUserAsync(ServiceExtensions.UserAddModel);

			_user1 = await userService.AddUserAsync(ServiceExtensions.UserAddModel1);
			var result = await _spaceService.AddAsync(_adminUser.Id, new SpaceAddModel
			{
				Name = "UY-1, PHILOSOPHY, L1",
				Identifier = "uy1_phi_l1"
			});
			_space = result.Item;

			

			var adminMemberModel = new MemberAddModel {UserId = _user1.Id, IsTeacher = true};
			_adminMember = (await _memberService.AddMemberAsync(_space, adminMemberModel, _adminUser)).Item;
			
			_model = new CourseAddModel
			{
				Name = "first name",
				Description = "description"
			};
		}

		[Test]
		public async Task GetById()
		{
			var course = (await _service.AddCourseAsync(_space, _model, _adminMember)).Item;
			var getResult = await _service.GetAsync(course.Id);

			Assert.AreEqual(course.Id, getResult.Id);
		}

		[Test]
		public void GetById_NotFound_ShouldThrow()
		{
			var ex = Assert.ThrowsAsync<ElementNotFoundException>(async () =>
			{
				await _service.GetAsync(ulong.MaxValue);
			});

			Assert.AreEqual("CourseNotFoundById", ex!.Code);
			Assert.AreEqual(ulong.MaxValue, ex.Params[0]);
		}


		[Test]
		public async Task AddCourseAsync()
		{
			var result = await _service.AddCourseAsync(_space, _model, _adminMember);
			var course = result.Item;
			await _dbContext.Entry(course).ReloadAsync();

			Assert.AreEqual(_space.Id, course.SpaceId);
			Assert.AreEqual(_model.Name, course.Name);
			Assert.AreEqual(StringHelper.Normalize(_model.Name), course.NormalizedName);
			Assert.AreEqual(_model.Description, course.Description);
			Assert.IsNotEmpty(course.PublisherId);
			Assert.IsNotEmpty(course.SubjectId);

			var assertions = _eventAssertionsBuilder.Build(result.Event)
				.HasName("COURSE_ADD")
				.HasData(new CourseDataModel(course));

			await assertions.HasActorIdAsync(_adminUser.ActorId);
			await assertions.HasActorIdAsync(_adminMember.ActorId);
			await assertions.HasPublisherIdAsync(_space.PublisherId);
			await assertions.HasPublisherIdAsync(course.PublisherId);
			await assertions.HasSubjectIdAsync(course.SubjectId);
		}


	
		[Test]
		public async Task TryAddCourse_WithUsedName_ShouldThrow()
		{
			await _service.AddCourseAsync(_space, _model, _adminMember);

			var ex = Assert.ThrowsAsync<UsedValueException>(async () =>
			{
				await _service.AddCourseAsync(_space, _model, _adminMember);
			});
			Assert.AreEqual("CourseNameUsed", ex!.Code);
			Assert.AreEqual(_space, ex.Params[0]);
			Assert.AreEqual(_model.Name, ex.Params[1]);
		}


		[Test]
		public async Task ChangeCourseName()
		{
			var course = (await _service.AddCourseAsync(_space, _model, _adminMember)).Item;
			var newName = "new course name";

			var eventData = new ChangeValueData<string>(course.Name, newName);
			var changeEvent = await _service.ChangeCourseNameAsync(course, newName, _adminMember);

			await _dbContext.Entry(course).ReloadAsync();

			Assert.AreEqual(newName, course.Name);
			Assert.AreEqual(StringHelper.Normalize(newName), course.NormalizedName);

			var assertions = _eventAssertionsBuilder.Build(changeEvent)
				.HasName("COURSE_CHANGE_NAME")
				.HasData(eventData);

			await assertions.HasActorIdAsync(_adminUser.ActorId);
			await assertions.HasActorIdAsync(_adminMember.ActorId);
			await assertions.HasPublisherIdAsync(_space.PublisherId);
			await assertions.HasPublisherIdAsync(course.PublisherId);
			await assertions.HasSubjectIdAsync(course.SubjectId);
		}


		[Test]
		public async Task TryChangeCourseName_WithUsedName_ShouldThrow()
		{
			var course = (await _service.AddCourseAsync(_space, _model, _adminMember)).Item;

			var ex = Assert.ThrowsAsync<UsedValueException>(async () =>
			{
				await _service.ChangeCourseNameAsync(course, course.Name, _adminMember);
			});
			Assert.AreEqual("CourseNameUsed", ex!.Message);
			Assert.AreEqual(course.Space, ex.Params[0]);
			Assert.AreEqual(course.Name, ex.Params[1]);
		}

		[Test]
		public async Task ChangeCourseDescription()
		{
			var course = (await _service.AddCourseAsync(_space, _model, _adminMember)).Item;
			var newDescription = "9632854";

			var eventData = new ChangeValueData<string>(course.Description, newDescription);
			var changeEvent = await _service.ChangeCourseDescriptionAsync(course, newDescription, _adminMember);

			await _dbContext.Entry(course).ReloadAsync();

			Assert.AreEqual(newDescription, course.Description);

			
			var assertions = _eventAssertionsBuilder.Build(changeEvent)
				.HasName("COURSE_CHANGE_DESCRIPTION")
				.HasData(eventData);

			await assertions.HasActorIdAsync(_adminUser.ActorId);
			await assertions.HasActorIdAsync(_adminMember.ActorId);
			await assertions.HasPublisherIdAsync(_space.PublisherId);
			await assertions.HasPublisherIdAsync(course.PublisherId);
			await assertions.HasSubjectIdAsync(course.SubjectId);
		}
		
		

		[Test]
		public async Task FindCourseByName()
		{
			var createdCourse = (await _service.AddCourseAsync(_space, _model, _adminMember)).Item;

			var course = await _service.GetByNameAsync(_space, createdCourse.Name);
			Assert.AreEqual(createdCourse.Id, course.Id);
		}

		[Test]
		public void FindCourseByName_NotFound_ShouldThrow()
		{
			var name = Guid.NewGuid().ToString();
			var ex = Assert.ThrowsAsync<ElementNotFoundException>(async () =>
			{
				await _service.GetByNameAsync(_space, name);
			});
			Assert.AreEqual("CourseNotFoundByName", ex!.Message);
			Assert.AreEqual(_space, ex.Params[0]);
			Assert.AreEqual(name, ex.Params[1]);
		}


		[Test]
		public async Task IsCourseByName()
		{
			var course = (await _service.AddCourseAsync(_space, _model, _adminMember)).Item;
			var hasCourse = await _service.ContainsByName(_space, course.Name);
			Assert.True(hasCourse);
		}


		[Test]
		public async Task IsCourseByName_WithNonCourse_ShouldBeFalse()
		{
			var hasCourse = await _service.ContainsByName(_space, "5D");
			Assert.False(hasCourse);
		}
		
	}
}