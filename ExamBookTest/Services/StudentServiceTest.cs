using System;
using System.Threading.Tasks;
using ExamBook.Entities;
using ExamBook.Identity;
using ExamBook.Identity.Models;
using ExamBook.Models;
using ExamBook.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Social.Helpers;
using Vx.Asserts;
using Vx.Models;
using Vx.Services;

#pragma warning disable NUnit2005
namespace ExamBookTest.Services
{
    public class StudentServiceTest
    {
        private IServiceProvider _provider = null!;
        private ClassroomService _classroomService = null!;
        private StudentService _service = null!;
        private SpaceService _spaceService = null!;
        private PublisherService _publisherService = null!;
        private EventAssertionsBuilder _eventAssertionsBuilder = null!;
        
        private DbContext _dbContext = null!;
        private User _adminUser = null!;
        private Actor _actor = null!;

        private Space _space = null!;
        private Classroom _classroom = null!;
        private ClassroomAddModel _classroomAddModel = null!;
        private StudentAddModel _model = null!;
            
        
        [SetUp]
        public async Task Setup()
        {
            var services = new ServiceCollection();
            
            _provider = services.Setup();
            _classroomService = _provider.GetRequiredService<ClassroomService>();
            _publisherService = _provider.GetRequiredService<PublisherService>();
            _eventAssertionsBuilder = _provider.GetRequiredService<EventAssertionsBuilder>();
            _dbContext = _provider.GetRequiredService<DbContext>();

            var userService = _provider.GetRequiredService<UserService>();
            _spaceService = _provider.GetRequiredService<SpaceService>();
            _service = _provider.GetRequiredService<StudentService>();
            _adminUser = await userService.AddUserAsync(ServiceExtensions.UserAddModel);
            _actor = await userService.GetActor(_adminUser);

            var result = await _spaceService.AddAsync(_adminUser.Id, new SpaceAddModel {
                Name = "UY-1, PHILOSOPHY, L1",
                Identifier = "uy1_phi_l1"
            });
            _space = result.Item;

            _classroomAddModel = new ClassroomAddModel { Name = "Classroom name" };
            _classroom = (await _classroomService.AddAsync(_space, _classroomAddModel, _adminUser)).Item;

            _model = new StudentAddModel
            {
                FirstName = "first name",
                LastName = "last name",
                RId = "8say6g3",
                BirthDate = new DateTime(1990, 1, 1),
                Sex = 'm'
            };
        }

     

        [Test]
        public async Task AddStudentAsync()
        {
            var result = await _service.AddAsync(_classroom, _model, _adminUser);
            var student = result.Item;
            await _dbContext.Entry(student).ReloadAsync();
            
            Assert.AreEqual(_classroom.Id, student.ClassroomId);
            Assert.AreEqual(_classroom.SpaceId, student.SpaceId);
            Assert.AreEqual(_model.FirstName, student.FirstName);
            Assert.AreEqual(_model.LastName, student.LastName);
            Assert.AreEqual(_model.Sex, student.Sex);
            Assert.AreEqual(_model.RId, student.RId);
            Assert.AreEqual(StringHelper.Normalize(_model.RId), student.NormalizedRId);
            Assert.IsNotEmpty(student.PublisherId);

            var publisher = await _publisherService.GetByIdAsync(student.PublisherId);
            var classroomPublisher = await _publisherService.GetByIdAsync(_classroom.PublisherId);
            var spacePublisher = await _publisherService.GetByIdAsync(_space.PublisherId);
            
            _eventAssertionsBuilder.Build(result.Event)
                .HasName("STUDENT_ADD")
                .HasActor(_actor)
                .HasPublisher(publisher)
                .HasPublisher(classroomPublisher)
                .HasPublisher(spacePublisher)
                .HasData(student);
        }
        
        
        
        
        [Test]
        public async Task DeleteStudentAsync()
        {
            var result = await _service.AddAsync(_classroom, _model, _adminUser);
            var student = result.Item;

            var deleteEvent = await _service.DeleteAsync(student, _adminUser);
            await _dbContext.Entry(student).ReloadAsync();
            
            Assert.NotNull(student.DeletedAt);
            Assert.AreEqual("", student.FirstName);
            Assert.AreEqual("", student.LastName);
            Assert.AreEqual('0', student.Sex);
            Assert.AreEqual("", student.RId);
            Assert.AreEqual("", student.NormalizedRId);

            var publisher = await _publisherService.GetByIdAsync(student.PublisherId);
            var classroomPublisher = await _publisherService.GetByIdAsync(_classroom.PublisherId);
            var spacePublisher = await _publisherService.GetByIdAsync(_space.PublisherId);
            
            _eventAssertionsBuilder.Build(deleteEvent)
                .HasName("STUDENT_DELETE")
                .HasActor(_actor)
                .HasPublisher(publisher)
                .HasPublisher(classroomPublisher)
                .HasPublisher(spacePublisher)
                .HasData(student);
        }
        
        
        
        
        [Test]
        public async Task IsStudent()
        {
            var student = (await _service.AddAsync(_classroom, _model, _adminUser)).Item;
            var hasStudent = await _service.ContainsAsync(_space, student.RId);
            Assert.True(hasStudent);
        }


        [Test]
        public async Task IsStudent_WithNonStudent_ShouldBeFalse()
        {
            var hasStudent = await _service.ContainsAsync(_space, "5D");
            Assert.False(hasStudent);
        }

        [Test]
        public async Task IsStudent_WithDeletedStudent_ShouldBeFalse()
        {
            var student = (await _service.AddAsync(_classroom, _model, _adminUser)).Item;
            await _service.DeleteAsync(student, _adminUser);
            
            var hasStudent = await _service.ContainsAsync(_space, student.RId);
            Assert.False(hasStudent);
        }

    }
}