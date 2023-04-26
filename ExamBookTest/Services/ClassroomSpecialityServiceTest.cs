using System;
using System.Threading.Tasks;
using ExamBook.Entities;
using ExamBook.Identity;
using ExamBook.Identity.Models;
using ExamBook.Models;
using ExamBook.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Vx.Asserts;
using Vx.Models;
using Vx.Services;

#pragma warning disable NUnit2005
namespace ExamBookTest.Services
{
    public class ClassroomSpecialityServiceTest
    {
        private IServiceProvider _provider = null!;
        private ClassroomService _classroomService = null!;
        private ClassroomSpecialityService _service = null!;
        private SpaceService _spaceService = null!;
        private RoomService _roomService = null!;
        private SpecialityService _specialityService = null!;
        private PublisherService _publisherService = null!;
        private EventAssertionsBuilder _eventAssertionsBuilder = null!;
        
        private DbContext _dbContext = null!;
        private User _adminUser = null!;
        private Actor _actor = null!;

        private Space _space = null!;
        private Room _room = null!;
        private Speciality _speciality = null!;
        private Classroom _classroom = null!;
        private ClassroomAddModel _classroomAddModel = null!;
            
        
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
            _roomService = _provider.GetRequiredService<RoomService>();
            _specialityService = _provider.GetRequiredService<SpecialityService>();
            _service = _provider.GetRequiredService<ClassroomSpecialityService>();
            _adminUser = await userService.AddUserAsync(ServiceExtensions.UserAddModel);
            _actor = await userService.GetActor(_adminUser);

            var result = await _spaceService.AddAsync(_adminUser.Id, new SpaceAddModel {
                Name = "UY-1, PHILOSOPHY, L1",
                Identifier = "uy1_phi_l1"
            });
            _space = result.Item;

            var roomModel = new RoomAddModel{Capacity = 10, Name = "Room name"};
            _room = (await _roomService.AddRoomAsync(_space, roomModel, _adminUser)).Item;

            var specialityModel = new SpecialityAddModel {Name = "speciality name"};
            _speciality = (await _specialityService.AddSpecialityAsync(_space, specialityModel, _adminUser)).Item;

            _classroomAddModel = new ClassroomAddModel { Name = "Classroom name" };
            _classroom = (await _classroomService.AddAsync(_space, _classroomAddModel, _adminUser)).Item;
        }

     

        [Test]
        public async Task AddClassroomSpecialityAsync()
        {
            var result = await _service.AddSpeciality(_classroom, _speciality, _adminUser);
            var item = result.Item;
            await _dbContext.Entry(item).ReloadAsync();
            
            Assert.AreEqual(_classroom.Id, item.ClassroomId);
            Assert.AreEqual(_speciality.Id, item.SpecialityId);
            Assert.NotNull(item.PublisherId);

            var publisher = await _publisherService.GetByIdAsync(item.PublisherId);
            var classroomPublisher = await _publisherService.GetByIdAsync(_classroom.PublisherId);
            var specialityPublisher = await _publisherService.GetByIdAsync(_speciality.PublisherId);
            var spacePublisher = await _publisherService.GetByIdAsync(_space.PublisherId);
            
            _eventAssertionsBuilder.Build(result.Event)
                .HasName("CLASSROOM_SPECIALITY_ADD")
                .HasActor(_actor)
                .HasPublisher(publisher)
                .HasPublisher(classroomPublisher)
                .HasPublisher(specialityPublisher)
                .HasPublisher(spacePublisher)
                .HasData(item);
        }
        
        
        
        
        [Test]
        public async Task DeleteClassroomSpecialityAsync()
        {
            var result = await _service.AddSpeciality(_classroom, _speciality, _adminUser);
            var classroomSpeciality = result.Item;

            var deleteEvent = await _service.DeleteSpecialityAsync(classroomSpeciality, _adminUser);
            await _dbContext.Entry(classroomSpeciality).ReloadAsync();
            
            Assert.NotNull(classroomSpeciality.DeletedAt);

            var publisher = await _publisherService.GetByIdAsync(classroomSpeciality.PublisherId);
            var classroomPublisher = await _publisherService.GetByIdAsync(_classroom.PublisherId);
            var specialityPublisher = await _publisherService.GetByIdAsync(_speciality.PublisherId);
            var spacePublisher = await _publisherService.GetByIdAsync(_space.PublisherId);
            
            _eventAssertionsBuilder.Build(deleteEvent)
                .HasName("CLASSROOM_SPECIALITY_DELETE")
                .HasActor(_actor)
                .HasPublisher(publisher)
                .HasPublisher(classroomPublisher)
                .HasPublisher(specialityPublisher)
                .HasPublisher(spacePublisher)
                .HasData(classroomSpeciality);
        }
        
        
        
        
        [Test]
        public async Task IsClassroomSpeciality()
        {
            await _service.AddSpeciality(_classroom, _speciality, _adminUser);
            var hasSpeciality = await _service.HasSpecialityAsync(_classroom, _speciality);
            Assert.True(hasSpeciality);
        }


        [Test]
        public async Task IsClassroomSpeciality_WithNonSpeciality_ShouldBeFalse()
        {
            var newSpeciality = (await _specialityService.AddSpecialityAsync(_space,
                new SpecialityAddModel {Name = "new speciality name"}, _adminUser)).Item;
            
            var hasSpeciality = await _service.HasSpecialityAsync(_classroom, newSpeciality);
            Assert.False(hasSpeciality);
        }

        [Test]
        public async Task IsSpeciality_WithDeletedSpeciality_ShouldBeFalse()
        {
            var item = (await _service.AddSpeciality(_classroom, _speciality, _adminUser)).Item;
            await _service.DeleteSpecialityAsync(item, _adminUser);
            
            var hasSpeciality = await _service.HasSpecialityAsync(_classroom, _speciality);
            Assert.False(hasSpeciality);
        }

    }
}