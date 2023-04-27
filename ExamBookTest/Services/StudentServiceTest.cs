using System;
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
        private StudentService _service = null!;
        private SpaceService _spaceService = null!;
        private PublisherService _publisherService = null!;
        private EventAssertionsBuilder _eventAssertionsBuilder = null!;
        
        private DbContext _dbContext = null!;
        private User _adminUser = null!;
        private Actor _actor = null!;

        private Space _space = null!;
        private StudentAddModel _model = null!;
            
        
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
            _service = _provider.GetRequiredService<StudentService>();
            _adminUser = await userService.AddUserAsync(ServiceExtensions.UserAddModel);
            _actor = await userService.GetActor(_adminUser);

            var result = await _spaceService.AddAsync(_adminUser.Id, new SpaceAddModel {
                Name = "UY-1, PHILOSOPHY, L1",
                Identifier = "uy1_phi_l1"
            });
            _space = result.Item;

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
            var result = await _service.AddAsync(_space, _model, _adminUser);
            var student = result.Item;
            await _dbContext.Entry(student).ReloadAsync();
            
            Assert.AreEqual(_space.Id, student.SpaceId);
            Assert.AreEqual(_model.FirstName, student.FirstName);
            Assert.AreEqual(_model.LastName, student.LastName);
            Assert.AreEqual(_model.Sex, student.Sex);
            Assert.AreEqual(_model.RId, student.Code);
            Assert.AreEqual(StringHelper.Normalize(_model.RId), student.NormalizedCode);
            Assert.IsNotEmpty(student.PublisherId);

            var publisher = await _publisherService.GetByIdAsync(student.PublisherId);
            var spacePublisher = await _publisherService.GetByIdAsync(_space.PublisherId);
            
            _eventAssertionsBuilder.Build(result.Event)
                .HasName("STUDENT_ADD")
                .HasActor(_actor)
                .HasPublisher(publisher)
                .HasPublisher(spacePublisher)
                .HasData(student);
        }
        
        
        
        
        [Test]
        public async Task DeleteStudentAsync()
        {
            var result = await _service.AddAsync(_space, _model, _adminUser);
            var student = result.Item;

            var deleteEvent = await _service.DeleteAsync(student, _adminUser);
            await _dbContext.Entry(student).ReloadAsync();
            
            Assert.NotNull(student.DeletedAt);
            Assert.AreEqual("", student.FirstName);
            Assert.AreEqual("", student.LastName);
            Assert.AreEqual('0', student.Sex);
            Assert.AreEqual("", student.Code);
            Assert.AreEqual("", student.NormalizedCode);

            var publisher = await _publisherService.GetByIdAsync(student.PublisherId);
            var spacePublisher = await _publisherService.GetByIdAsync(_space.PublisherId);
            
            _eventAssertionsBuilder.Build(deleteEvent)
                .HasName("STUDENT_DELETE")
                .HasActor(_actor)
                .HasPublisher(publisher)
                .HasPublisher(spacePublisher)
                .HasData(student);
        }
        
        [Test]
        public async Task ChangeStudentCode()
        {
            Student student = (await _service.AddAsync (_space, _model, _adminUser)).Item;
            var newCode = "9632854";

            var eventData = new ChangeValueData<string>(student.Code, newCode);
            var changeEvent = await _service.ChangeCodeAsync(student, newCode, _adminUser);

            await _dbContext.Entry(student).ReloadAsync();

            Assert.AreEqual(newCode, student.Code);
            Assert.AreEqual(StringHelper.Normalize(newCode), student.NormalizedCode);

            var publisher = await _publisherService.GetByIdAsync(student.PublisherId);
            var spacePublisher = await _publisherService.GetByIdAsync(_space.PublisherId);

            Assert.NotNull(publisher);
            _eventAssertionsBuilder.Build(changeEvent)
                .HasName("STUDENT_CHANGE_CODE")
                .HasActor(_actor)
                .HasPublisher(publisher)
                .HasPublisher(spacePublisher)
                .HasData(eventData);
        }
        
        
        [Test]
        public async Task TryChangeStudentCode_WithUsedCode_ShouldThrow()
        {
            var student = (await _service.AddAsync(_space, _model, _adminUser)).Item;

            var ex = Assert.ThrowsAsync<UsedValueException>(async () =>
            {
                await _service.ChangeCodeAsync(student, student.Code, _adminUser);
            });
            Assert.AreEqual("StudentCodeUsed", ex!.Message);
        }
        
        
        [Test]
        public async Task IsStudent()
        {
            var student = (await _service.AddAsync(_space, _model, _adminUser)).Item;
            var hasStudent = await _service.ContainsAsync(_space, student.Code);
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
            var student = (await _service.AddAsync(_space, _model, _adminUser)).Item;
            var rId = student.Code;
            
            await _service.DeleteAsync(student, _adminUser);
            
            var hasStudent = await _service.ContainsAsync(_space, rId);
            Assert.False(hasStudent);
        }

    }
}