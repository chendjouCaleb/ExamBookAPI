using System;
using System.Collections.Generic;
using System.Linq;
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
    public class StudentSpecialityServiceTest
    {
        private IServiceProvider _provider = null!;
        private StudentService _studentService = null!;
        private StudentSpecialityService _service = null!;
        private SpaceService _spaceService = null!;
        private SpecialityService _specialityService = null!;
        private PublisherService _publisherService = null!;
        private EventAssertionsBuilder _eventAssertionsBuilder = null!;

        private DbContext _dbContext = null!;
        private User _adminUser = null!;
        private Actor _actor = null!;

        private Space _space = null!;
        private Speciality _speciality1 = null!;
        private Speciality _speciality2 = null!;
        private ICollection<Speciality> _specialities = null!;
        private StudentAddModel _model = null!;
        private Student _student = null!;


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
            _studentService = _provider.GetRequiredService<StudentService>();
            _specialityService = _provider.GetRequiredService<SpecialityService>();
            _service = _provider.GetRequiredService<StudentSpecialityService>();
            _adminUser = await userService.AddUserAsync(ServiceExtensions.UserAddModel);
            _actor = await userService.GetActor(_adminUser);

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

        _model = new StudentAddModel
            {
                FirstName = "first name",
                LastName = "last name",
                RId = "652",
                BirthDate = new DateTime(1990, 1, 1)
            };
        }


        [Test]
        public async Task AddStudentAsync()
        {
            _model.SpecialityIds = _specialities.Select(s => s.Id).ToHashSet();
            var result = await _studentService.AddAsync(_space, _model, _adminUser);
            var student = result.Item;
            await _dbContext.Entry(student).ReloadAsync();
            
            Assert.IsNotEmpty(student.PublisherId);

            var publisher = await _publisherService.GetByIdAsync(student.PublisherId);
            var spacePublisher = await _publisherService.GetByIdAsync(_space.PublisherId);
            var speciality1Publisher = await _publisherService.GetByIdAsync(_speciality1.PublisherId);
            var speciality2Publisher = await _publisherService.GetByIdAsync(_speciality2.PublisherId);

            _eventAssertionsBuilder.Build(result.Event)
                .HasName("STUDENT_ADD")
                .HasActor(_actor)
                .HasPublisher(publisher)
                .HasPublisher(spacePublisher)
                .HasPublisher(speciality1Publisher)
                .HasPublisher(speciality2Publisher)
                .HasData(student);
        }
        


        [Test]
        public async Task AddStudentSpeciality()
        {
            var student = (await _studentService.AddAsync(_space, _model, _adminUser)).Item;
            var result = await _service.AddSpecialityAsync(student, _speciality1, _adminUser);
            var studentSpeciality = result.Item;
            await _dbContext.Entry(studentSpeciality).ReloadAsync();

            Assert.AreEqual(student.Id, studentSpeciality.StudentId);
            Assert.AreEqual(_speciality1.Id, studentSpeciality.SpecialityId);

            var publisher = await _publisherService.GetByIdAsync(student.PublisherId);
            var specialityPublisher = await _publisherService.GetByIdAsync(_speciality1.PublisherId);
            var spacePublisher = await _publisherService.GetByIdAsync(_space.PublisherId);

            _eventAssertionsBuilder.Build(result.Event)
                .HasName("STUDENT_SPECIALITY_ADD")
                .HasActor(_actor)
                .HasPublisher(publisher)
                .HasPublisher(spacePublisher)
                .HasPublisher(specialityPublisher)
                .HasData(studentSpeciality);
        }


        [Test]
        public async Task AddStudentSpecialities()
        {
            var student = (await _studentService.AddAsync(_space, _model, _adminUser)).Item;
            var result = await _service.AddSpecialitiesAsync(student, _specialities, _adminUser);
            
            var studentSpecialities = result.Item;

            foreach (var speciality in _specialities)
            {
                var studentSpeciality = studentSpecialities.First(cs => cs.SpecialityId == speciality.Id);
                Assert.AreEqual(student.Id, studentSpeciality.StudentId);
            }
            
            var publisher = await _publisherService.GetByIdAsync(student.PublisherId);
            var speciality1Publisher = await _publisherService.GetByIdAsync(_speciality1.PublisherId);
            var speciality2Publisher = await _publisherService.GetByIdAsync(_speciality2.PublisherId);
            var spacePublisher = await _publisherService.GetByIdAsync(_space.PublisherId);

            _eventAssertionsBuilder.Build(result.Event)
                .HasName("STUDENT_SPECIALITIES_ADD")
                .HasActor(_actor)
                .HasPublisher(publisher)
                .HasPublisher(spacePublisher)
                .HasPublisher(speciality1Publisher)
                .HasPublisher(speciality2Publisher)
                .HasData(studentSpecialities);
        }
        
        
        [Test]
        public async Task DeleteStudentSpeciality()
        {
            var student = (await _studentService.AddAsync(_space, _model, _adminUser)).Item;
            var studentSpeciality = (await _service.AddSpecialityAsync(student, _speciality1, _adminUser)).Item;
            var @event = await _service.DeleteSpeciality(studentSpeciality, _adminUser);
            await _dbContext.Entry(studentSpeciality).ReloadAsync();

            Assert.NotNull(studentSpeciality.DeletedAt);

            var publisher = await _publisherService.GetByIdAsync(student.PublisherId);
            var specialityPublisher = await _publisherService.GetByIdAsync(_speciality1.PublisherId);
            var spacePublisher = await _publisherService.GetByIdAsync(_space.PublisherId);

            _eventAssertionsBuilder.Build(@event)
                .HasName("STUDENT_SPECIALITY_DELETE")
                .HasActor(_actor)
                .HasPublisher(publisher)
                .HasPublisher(spacePublisher)
                .HasPublisher(specialityPublisher)
                .HasData(studentSpeciality);
        }
        

        [Test]
        public async Task StudentSpecialityExists()
        {
            var student = (await _studentService.AddAsync(_space, _model, _adminUser)).Item;
            await _service.AddSpecialityAsync(student, _speciality1, _adminUser);

            var exists = await _service.ContainsAsync(student, _speciality1);
            Assert.True(exists);
        }
        
        [Test]
        public async Task StudentSpecialityExistns_WithNonSpecialityStudent_ShouldBeFalse()
        {
            var student = (await _studentService.AddAsync(_space, _model, _adminUser)).Item;

            var exists = await _service.ContainsAsync(student, _speciality1);
            Assert.False(exists);
        }
        
        
        [Test]
        public async Task StudentSpecialityExists_WithDeleted_ShouldBeFalse()
        {
            var student = (await _studentService.AddAsync(_space, _model, _adminUser)).Item;
            var studentSpeciality = (await _service.AddSpecialityAsync(student, _speciality1, _adminUser)).Item;
            await _service.DeleteSpeciality(studentSpeciality, _adminUser);

            var exists = await _service.ContainsAsync(student, _speciality1);
            Assert.False(exists);
        }
        
        
    }
}