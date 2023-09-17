using System;
using System.Collections.Generic;
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
using Traceability.Models;
using Traceability.Services;

#pragma warning disable NUnit2005
namespace ExamBookTest.Services
{
    public class CourseClassroomServiceTest
    {
        private IServiceProvider _provider = null!;
        private CourseService _service = null!;
        private CourseTeacherService _courseTeacherService = null!;
        private SpaceService _spaceService = null!;
        private MemberService _memberService = null!;
        private SpecialityService _specialityService = null!;
        private PublisherService _publisherService = null!;
        private EventAssertionsBuilder _eventAssertionsBuilder = null!;

        private DbContext _dbContext = null!;
        private User _adminUser = null!;
        private User _user1 = null!;
        private User _user2 = null!;

        private Member _member1 = null!;
        private Member _member2 = null!;
        private Member _adminMember = null!;

        private Space _space = null!;
        
        private Speciality _speciality1 = null!;
        private Speciality _speciality2 = null!;

        private ICollection<Speciality> _specialities = null!;
        private CourseClassroomAddModel _model = null!;


        [SetUp]
        public async Task Setup()
        {
            var services = new ServiceCollection();

            _provider = services.Setup();
            _publisherService = _provider.GetRequiredService<PublisherService>();
            _eventAssertionsBuilder = _provider.GetRequiredService<EventAssertionsBuilder>();
            _dbContext = _provider.GetRequiredService<DbContext>();

            var userService = _provider.GetRequiredService<UserService>();
            _spaceService = _provider.GetRequiredService<SpaceService>();
            _specialityService = _provider.GetRequiredService<SpecialityService>();
            _memberService = _provider.GetRequiredService<MemberService>();
            _courseTeacherService = _provider.GetRequiredService<CourseTeacherService>();
            _service = _provider.GetRequiredService<CourseService>();
            _adminUser = await userService.AddUserAsync(ServiceExtensions.UserAddModel);
            _actor = await userService.GetActor(_adminUser);
            
            _user1 = await userService.AddUserAsync(ServiceExtensions.UserAddModel1);
            _user2 = await userService.AddUserAsync(ServiceExtensions.UserAddModel2);
            
            var result = await _spaceService.AddAsync(_adminUser.Id, new SpaceAddModel
            {
                Name = "UY-1, PHILOSOPHY, L1",
                Identifier = "uy1_phi_l1"
            });
            _space = result.Item;

            var specialityModel1 = new SpecialityAddModel {Name = "speciality name1"};
            _speciality1 = (await _specialityService.AddSpecialityAsync(_space, specialityModel1, _adminUser)).Item;

            var specialityModel2 = new SpecialityAddModel {Name = "speciality name2"};
            _speciality2 = (await _specialityService.AddSpecialityAsync(_space, specialityModel2, _adminUser)).Item;
            _specialities = new List<Speciality>{_speciality1, _speciality2};
            
            var memberModel1 = new MemberAddModel {UserId = _user1.Id, IsTeacher = true};
            _member1 = (await _memberService.AddMemberAsync(_space, memberModel1, _adminUser)).Item;

            var memberModel2 = new MemberAddModel {UserId = _user2.Id, IsTeacher = true};
            _member2 = (await _memberService.AddMemberAsync(_space, memberModel2, _adminUser)).Item;

            

        _model = new CourseAddModel
            {
                Name = "first name",
                Code = "652",
                Coefficient = 12,
                Description = "description"
            };
        }

        [Test]
        public async Task GetById()
        {
            var course = (await _service.AddCourseAsync(_space, _model, _adminUser)).Item;
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
            _model.MemberIds = new HashSet<ulong> {_member1.Id, _member2.Id};
            var result = await _service.AddCourseAsync(_space, _model, _adminUser);
            var course = result.Item;
            await _dbContext.Entry(course).ReloadAsync();

            Assert.AreEqual(_space.Id, course.SpaceId);
            Assert.AreEqual(_model.Name, course.Name);
            Assert.AreEqual(StringHelper.Normalize(_model.Name), course.NormalizedName);
            Assert.AreEqual(_model.Code, course.Code);
            Assert.AreEqual(StringHelper.Normalize(_model.Code), course.NormalizedCode);
            Assert.AreEqual(_model.Coefficient, course.Coefficient);
            Assert.NotZero(course.Coefficient);
            Assert.AreEqual(_model.Description, course.Description);
            Assert.IsNotEmpty(course.PublisherId);
            
            Assert.True(await _courseTeacherService.ContainsAsync(course, _member1));
            Assert.True(await _courseTeacherService.ContainsAsync(course, _member2));

            var publisher = await _publisherService.GetByIdAsync(course.PublisherId);
            var spacePublisher = await _publisherService.GetByIdAsync(_space.PublisherId);

            _eventAssertionsBuilder.Build(result.Event)
                .HasName("COURSE_ADD")
                .HasActor(_actor)
                .HasPublisher(publisher)
                .HasPublisher(spacePublisher)
                .HasData(course);
        }


        [Test]
        public async Task TryAddCourse_WithUsedCode_ShouldThrow()
        {
            await _service.AddCourseAsync(_space, _model, _adminUser);
            _model.Name = Guid.NewGuid().ToString();

            var ex = Assert.ThrowsAsync<UsedValueException>(async () =>
            {
                await _service.AddCourseAsync(_space, _model, _adminUser);
            });
            Assert.AreEqual("CourseCodeUsed", ex!.Code);
            Assert.AreEqual(_model.Code, ex.Params[0]);
        }

        [Test]
        public async Task TryAddCourse_WithUsedName_ShouldThrow()
        {
            await _service.AddCourseAsync(_space, _model, _adminUser);
            _model.Code = Guid.NewGuid().ToString();

            var ex = Assert.ThrowsAsync<UsedValueException>(async () =>
            {
                await _service.AddCourseAsync(_space, _model, _adminUser);
            });
            Assert.AreEqual("CourseNameUsed", ex!.Code);
            Assert.AreEqual(_model.Name, ex.Params[0]);
        }


        [Test]
        public async Task ChangeCourseCode()
        {
            var course = (await _service.AddCourseAsync(_space, _model, _adminUser)).Item;
            var newCode = "9632854";

            var eventData = new ChangeValueData<string>(course.Code, newCode);
            var changeEvent = await _service.ChangeCourseCodeAsync(course, newCode, _adminUser);

            await _dbContext.Entry(course).ReloadAsync();

            Assert.AreEqual(newCode, course.Code);
            Assert.AreEqual(StringHelper.Normalize(newCode), course.NormalizedCode);

            var publisher = await _publisherService.GetByIdAsync(course.PublisherId);
            var spacePublisher = await _publisherService.GetByIdAsync(_space.PublisherId);

            Assert.NotNull(publisher);
            _eventAssertionsBuilder.Build(changeEvent)
                .HasName("COURSE_CHANGE_CODE")
                .HasActor(_actor)
                .HasPublisher(publisher)
                .HasPublisher(spacePublisher)
                .HasData(eventData);
        }


        [Test]
        public async Task TryChangeCourseCode_WithUsedCode_ShouldThrow()
        {
            var course = (await _service.AddCourseAsync(_space, _model, _adminUser)).Item;

            var ex = Assert.ThrowsAsync<UsedValueException>(async () =>
            {
                await _service.ChangeCourseCodeAsync(course, course.Code, _adminUser);
            });
            Assert.AreEqual("CourseCodeUsed", ex!.Code);
            Assert.AreEqual(course.Code, ex.Params[0]);
        }
        
        [Test]
        public async Task ChangeCourseName()
        {
            var course = (await _service.AddCourseAsync(_space, _model, _adminUser)).Item;
            var newName = "9632854";

            var eventData = new ChangeValueData<string>(course.Name, newName);
            var changeEvent = await _service.ChangeCourseNameAsync(course, newName, _adminUser);

            await _dbContext.Entry(course).ReloadAsync();

            Assert.AreEqual(newName, course.Name);
            Assert.AreEqual(StringHelper.Normalize(newName), course.NormalizedName);

            var publisher = await _publisherService.GetByIdAsync(course.PublisherId);
            var spacePublisher = await _publisherService.GetByIdAsync(_space.PublisherId);

            Assert.NotNull(publisher);
            _eventAssertionsBuilder.Build(changeEvent)
                .HasName("COURSE_CHANGE_NAME")
                .HasActor(_actor)
                .HasPublisher(publisher)
                .HasPublisher(spacePublisher)
                .HasData(eventData);
        }

        
        [Test]
        public async Task TryChangeCourseName_WithUsedName_ShouldThrow()
        {
            var course = (await _service.AddCourseAsync(_space, _model, _adminUser)).Item;

            var ex = Assert.ThrowsAsync<UsedValueException>(async () =>
            {
                await _service.ChangeCourseNameAsync(course, course.Name, _adminUser);
            });
            Assert.AreEqual("CourseNameUsed", ex!.Message);
            Assert.AreEqual(course.Name, ex.Params[0]);
        }
        
        [Test]
        public async Task ChangeCourseDescription()
        {
            var course = (await _service.AddCourseAsync(_space, _model, _adminUser)).Item;
            var newDescription = "9632854";

            var eventData = new ChangeValueData<string>(course.Description, newDescription);
            var changeEvent = await _service.ChangeCourseDescriptionAsync(course, newDescription, _adminUser);

            await _dbContext.Entry(course).ReloadAsync();

            Assert.AreEqual(newDescription, course.Description);

            var publisher = await _publisherService.GetByIdAsync(course.PublisherId);
            var spacePublisher = await _publisherService.GetByIdAsync(_space.PublisherId);

            Assert.NotNull(publisher);
            _eventAssertionsBuilder.Build(changeEvent)
                .HasName("COURSE_CHANGE_DESCRIPTION")
                .HasActor(_actor)
                .HasPublisher(publisher)
                .HasPublisher(spacePublisher)
                .HasData(eventData);
        }


        [Test]
        public async Task ChangeCourseCoefficient()
        {
            var course = (await _service.AddCourseAsync(_space, _model, _adminUser)).Item;
            uint newCoefficient = 20;

            var eventData = new ChangeValueData<uint>(course.Coefficient, newCoefficient);
            var changeEvent = await _service.ChangeCourseCoefficientAsync(course, newCoefficient, _adminUser);

            await _dbContext.Entry(course).ReloadAsync();

            Assert.AreEqual(newCoefficient, course.Coefficient);

            var publisher = await _publisherService.GetByIdAsync(course.PublisherId);
            var spacePublisher = await _publisherService.GetByIdAsync(_space.PublisherId);

            Assert.NotNull(publisher);
            _eventAssertionsBuilder.Build(changeEvent)
                .HasName("COURSE_CHANGE_COEFFICIENT")
                .HasActor(_actor)
                .HasPublisher(publisher)
                .HasPublisher(spacePublisher)
                .HasData(eventData);
        }

        [Test]
        public async Task DeleteCourseAsync()
        {
            var result = await _service.AddCourseAsync(_space, _model, _adminUser);
            var course = result.Item;

            var deleteEvent = await _service.DeleteAsync(course, _adminUser);
            await _dbContext.Entry(course).ReloadAsync();

            Assert.NotNull(course.DeletedAt);
            Assert.AreEqual("", course.Name);
            Assert.AreEqual("", course.NormalizedName);
            Assert.AreEqual("", course.Code);
            Assert.AreEqual("", course.NormalizedCode);
            Assert.AreEqual("", course.Description);
            Assert.AreEqual(0, course.Coefficient);

            var publisher = await _publisherService.GetByIdAsync(course.PublisherId);
            var spacePublisher = await _publisherService.GetByIdAsync(_space.PublisherId);

            _eventAssertionsBuilder.Build(deleteEvent)
                .HasName("COURSE_DELETE")
                .HasActor(_actor)
                .HasPublisher(publisher)
                .HasPublisher(spacePublisher)
                .HasData(course);
        }


        [Test]
        public async Task FindCourseByCode()
        {
            var createdCourse = (await _service.AddCourseAsync(_space, _model, _adminUser)).Item;

            var course = await _service.GetCourseByCodeAsync(_space, createdCourse.Code);
            Assert.AreEqual(createdCourse.Id, course.Id);
        }

        [Test]
        public void FindCourseByCode_NotFound_ShouldThrow()
        {
            var code = Guid.NewGuid().ToString();
            var ex = Assert.ThrowsAsync<ElementNotFoundException>(async () =>
            {
                await _service.GetCourseByCodeAsync(_space, code);
            });
            Assert.AreEqual("CourseNotFoundByCode", ex!.Message);
            Assert.AreEqual(code, ex.Params[0]);
        }


        [Test]
        public async Task FindCourseByName()
        {
            var createdCourse = (await _service.AddCourseAsync(_space, _model, _adminUser)).Item;

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
            Assert.AreEqual(name, ex.Params[0]);
        }


        [Test]
        public async Task IsCourseByCode()
        {
            var course = (await _service.AddCourseAsync(_space, _model, _adminUser)).Item;
            var hasCourse = await _service.ContainsByCode(_space, course.Code);
            Assert.True(hasCourse);
        }


        [Test]
        public async Task IsCourseByCode_WithNonCourse_ShouldBeFalse()
        {
            var hasCourse = await _service.ContainsByCode(_space, "5D");
            Assert.False(hasCourse);
        }


        [Test]
        public async Task IsCourseByName()
        {
            var course = (await _service.AddCourseAsync(_space, _model, _adminUser)).Item;
            var hasCourse = await _service.ContainsByName(_space, course.Name);
            Assert.True(hasCourse);
        }


        [Test]
        public async Task IsCourseByName_WithNonCourse_ShouldBeFalse()
        {
            var hasCourse = await _service.ContainsByName(_space, "5D");
            Assert.False(hasCourse);
        }


        [Test]
        public async Task AddCourseSpeciality()
        {
            var course = (await _service.AddCourseAsync(_space, _model, _adminUser)).Item;
            var result = await _service.AddCourseSpecialityAsync(course, _speciality1, _adminUser);
            var courseSpeciality = result.Item;
            await _dbContext.Entry(courseSpeciality).ReloadAsync();

            Assert.AreEqual(course.Id, courseSpeciality.CourseId);
            Assert.AreEqual(_speciality1.Id, courseSpeciality.SpecialityId);

            var publisher = await _publisherService.GetByIdAsync(course.PublisherId);
            var specialityPublisher = await _publisherService.GetByIdAsync(_speciality1.PublisherId);
            var spacePublisher = await _publisherService.GetByIdAsync(_space.PublisherId);

            _eventAssertionsBuilder.Build(result.Event)
                .HasName("COURSE_SPECIALITY_ADD")
                .HasActor(_actor)
                .HasPublisher(publisher)
                .HasPublisher(spacePublisher)
                .HasPublisher(specialityPublisher)
                .HasData(courseSpeciality);
        }


        [Test]
        public async Task AddCourseSpecialities()
        {
            var course = (await _service.AddCourseAsync(_space, _model, _adminUser)).Item;
            var result = await _service.AddCourseSpecialitiesAsync(course, _specialities, _adminUser);
            
            var courseSpecialities = result.Item;

            foreach (var speciality in _specialities)
            {
                var courseSpeciality = courseSpecialities.First(cs => cs.SpecialityId == speciality.Id);
                Assert.AreEqual(course.Id, courseSpeciality.CourseId);
            }
            
            var publisher = await _publisherService.GetByIdAsync(course.PublisherId);
            var speciality1Publisher = await _publisherService.GetByIdAsync(_speciality1.PublisherId);
            var speciality2Publisher = await _publisherService.GetByIdAsync(_speciality2.PublisherId);
            var spacePublisher = await _publisherService.GetByIdAsync(_space.PublisherId);

            _eventAssertionsBuilder.Build(result.Event)
                .HasName("COURSE_SPECIALITIES_ADD")
                .HasActor(_actor)
                .HasPublisher(publisher)
                .HasPublisher(spacePublisher)
                .HasPublisher(speciality1Publisher)
                .HasPublisher(speciality2Publisher)
                .HasData(courseSpecialities);
        }
        
        
        [Test]
        public async Task DeleteCourseSpeciality()
        {
            var course = (await _service.AddCourseAsync(_space, _model, _adminUser)).Item;
            var courseSpeciality = (await _service.AddCourseSpecialityAsync(course, _speciality1, _adminUser)).Item;
            var @event = await _service.DeleteCourseSpecialityAsync(courseSpeciality, _adminUser);
            await _dbContext.Entry(courseSpeciality).ReloadAsync();

            Assert.NotNull(courseSpeciality.DeletedAt);

            var publisher = await _publisherService.GetByIdAsync(course.PublisherId);
            var specialityPublisher = await _publisherService.GetByIdAsync(_speciality1.PublisherId);
            var spacePublisher = await _publisherService.GetByIdAsync(_space.PublisherId);

            _eventAssertionsBuilder.Build(@event)
                .HasName("COURSE_SPECIALITY_DELETE")
                .HasActor(_actor)
                .HasPublisher(publisher)
                .HasPublisher(spacePublisher)
                .HasPublisher(specialityPublisher)
                .HasData(courseSpeciality);
        }
        

        [Test]
        public async Task CourseSpecialityExists()
        {
            var course = (await _service.AddCourseAsync(_space, _model, _adminUser)).Item;
            await _service.AddCourseSpecialityAsync(course, _speciality1, _adminUser);

            var exists = await _service.CourseSpecialityExists(course, _speciality1);
            Assert.True(exists);
        }
        
        
        [Test]
        public async Task CourseSpecialityExists_WithDeleted_ShouldBeFalse()
        {
            var course = (await _service.AddCourseAsync(_space, _model, _adminUser)).Item;
            var courseSpeciality = (await _service.AddCourseSpecialityAsync(course, _speciality1, _adminUser)).Item;
            await _service.DeleteCourseSpecialityAsync(courseSpeciality, _adminUser);

            var exists = await _service.CourseSpecialityExists(course, _speciality1);
            Assert.False(exists);
        }
        
        
    }
}