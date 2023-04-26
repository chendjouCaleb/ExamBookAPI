﻿using System;
using System.Threading.Tasks;
using ExamBook.Entities;
using ExamBook.Exceptions;
using ExamBook.Identity;
using ExamBook.Identity.Models;
using ExamBook.Models;
using ExamBook.Models.Data;
using ExamBook.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities;
using Social.Helpers;
using Vx.Asserts;
using Vx.Models;
using Vx.Services;

#pragma warning disable NUnit2005
namespace ExamBookTest.Services
{
    public class CourseServiceTest
    {
        private IServiceProvider _provider = null!;
        private CourseService _service = null!;
        private SpaceService _spaceService = null!;
        private PublisherService _publisherService = null!;
        private EventAssertionsBuilder _eventAssertionsBuilder = null!;
        
        private DbContext _dbContext = null!;
        private User _adminUser = null!;
        private Actor _actor = null!;

        private Space _space = null!;
        private CourseAddModel _model = null!;
            
        
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
            _service = _provider.GetRequiredService<CourseService>();
            _adminUser = await userService.AddUserAsync(ServiceExtensions.UserAddModel);
            _actor = await userService.GetActor(_adminUser);

            var result = await _spaceService.AddAsync(_adminUser.Id, new SpaceAddModel {
                Name = "UY-1, PHILOSOPHY, L1",
                Identifier = "uy1_phi_l1"
            });
            _space = result.Item;

            _model = new CourseAddModel
            {
                Name = "first name",
                Code = "652",
                Coefficient = 12,
                Description = "description"
            };
        }

     

        [Test]
        public async Task AddCourseAsync()
        {
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
            Assert.AreEqual("CourseCodeUsed", ex!.Message);
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
            Assert.AreEqual("CourseNameUsed", ex!.Message);
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

            var course = await _service.FindCourseByCodeAsync(_space, createdCourse.Code);
            Assert.AreEqual(createdCourse.Id, course.Id);
        }

        [Test]
        public void FindCourseByCode_NotFound_ShouldThrow()
        {
            var ex = Assert.ThrowsAsync<ElementNotFoundException>(async () =>
            {
                await _service.FindCourseByCodeAsync(_space, Guid.NewGuid().ToString());
            });
            Assert.AreEqual("CourseNotFoundByCode", ex!.Message);
        }
        
        
        [Test]
        public async Task FindCourseByName()
        {
            var createdCourse = (await _service.AddCourseAsync(_space, _model, _adminUser)).Item;

            var course = await _service.FindCourseByNameAsync(_space, createdCourse.Name);
            Assert.AreEqual(createdCourse.Id, course.Id);
        }

        [Test]
        public void FindCourseByName_NotFound_ShouldThrow()
        {
            var ex = Assert.ThrowsAsync<ElementNotFoundException>(async () =>
            {
                await _service.FindCourseByNameAsync(_space, Guid.NewGuid().ToString());
            });
            Assert.AreEqual("CourseNotFoundByName", ex!.Message);
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
    }
}