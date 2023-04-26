using System;
using System.Threading.Tasks;
using ExamBook.Entities;
using ExamBook.Exceptions;
using ExamBook.Helpers;
using ExamBook.Identity;
using ExamBook.Identity.Models;
using ExamBook.Models;
using ExamBook.Models.Data;
using ExamBook.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Vx.Asserts;
using Vx.Models;
using Vx.Services;

#pragma warning disable NUnit2005
namespace ExamBookTest.Services
{
    public class ClassroomServiceTest
    {
        private IServiceProvider _provider = null!;
        private ClassroomService _classroomService = null!;
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
        private ClassroomAddModel _model = null!;
            
        
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

            _model = new ClassroomAddModel
            {
                Name = "Classroom name"
            };
        }


        [Test]
        public async Task AddClassroom()
        {
            var result = await _classroomService.AddAsync(_space, _model, _adminUser);
            var classroom = result.Item;
            
            await _dbContext.Entry(classroom).ReloadAsync();
            
            Assert.AreEqual(_model.Name, classroom.Name);
            Assert.AreEqual(StringHelper.Normalize(_model.Name), classroom.NormalizedName);
            Assert.AreEqual(_space.Id, classroom.SpaceId);

            var publisher = await _publisherService.GetByIdAsync(classroom.PublisherId);
            var spacePublisher = await _publisherService.GetByIdAsync(_space.PublisherId);
            var addEvent = result.Event;

            Assert.NotNull(publisher);
            _eventAssertionsBuilder.Build(addEvent)
                .HasName("CLASSROOM_ADD")
                .HasActor(_actor)
                .HasPublisher(publisher)
                .HasPublisher(spacePublisher)
                .HasData(classroom);
        }
        
        
        [Test]
        public async Task AddClassroomWithRoom()
        {
            _model.RoomId = _room.Id;
            var result = await _classroomService.AddAsync(_space, _model, _adminUser);
            var classroom = result.Item;
            
            await _dbContext.Entry(classroom).ReloadAsync();
            
            Assert.AreEqual(_model.Name, classroom.Name);
            Assert.AreEqual(StringHelper.Normalize(_model.Name), classroom.NormalizedName);
            Assert.AreEqual(_space.Id, classroom.SpaceId);
            Assert.AreEqual(_room.Id, classroom.RoomId);

            var publisher = await _publisherService.GetByIdAsync(classroom.PublisherId);
            var spacePublisher = await _publisherService.GetByIdAsync(_space.PublisherId);
            var roomPublisher = await _publisherService.GetByIdAsync(_room.PublisherId);
            var addEvent = result.Event;

            Assert.NotNull(publisher);
            _eventAssertionsBuilder.Build(addEvent)
                .HasName("CLASSROOM_ADD")
                .HasActor(_actor)
                .HasPublisher(publisher)
                .HasPublisher(spacePublisher)
                .HasPublisher(roomPublisher)
                .HasData(classroom);
        }

        
        [Test]
        public async Task TryAddClassroomWithUsedName_ShouldThrow()
        {
            await _classroomService.AddAsync(_space, _model, _adminUser);

            var ex = Assert.ThrowsAsync<UsedValueException>(async () =>
            {
                await _classroomService.AddAsync(_space, _model, _adminUser);
            });
            
            Assert.AreEqual("ClassroomNameUsed", ex!.Message);
        }
        
        [Test]
        public async Task ChangeClassroomName()
        {
            var result = await _classroomService.AddAsync(_space, _model, _adminUser);
            var classroom = result.Item;

            var newName = "new classroom name";
            var eventData = new ChangeValueData<string>(classroom.Name, newName);
            var changeEvent = await _classroomService.ChangeNameAsync(classroom, newName, _adminUser);
            
            await _dbContext.Entry(classroom).ReloadAsync();
            
            Assert.AreEqual(newName, classroom.Name);
            Assert.AreEqual(StringHelper.Normalize(newName), classroom.NormalizedName);

            var publisher = await _publisherService.GetByIdAsync(classroom.PublisherId);
            var spacePublisher = await _publisherService.GetByIdAsync(_space.PublisherId);

            Assert.NotNull(publisher);
            _eventAssertionsBuilder.Build(changeEvent)
                .HasName("CLASSROOM_CHANGE_NAME")
                .HasActor(_actor)
                .HasPublisher(publisher)
                .HasPublisher(spacePublisher)
                .HasData(eventData);
        }
        
        
        [Test]
        public async Task SetClassroomRoom()
        {
            var result = await _classroomService.AddAsync(_space, _model, _adminUser);
            var classroom = result.Item;

            var newRoom = (await _roomService.AddRoomAsync(_space, 
                new RoomAddModel { Name = "room23", Capacity = 20}, _adminUser)).Item;
            
            var eventData = new ChangeValueData<ulong>(0, newRoom.Id);
            var changeEvent = await _classroomService.ChangeRoomAsync(classroom, newRoom, _adminUser);
            
            await _dbContext.Entry(classroom).ReloadAsync();
            
            Assert.AreEqual(newRoom.Id, classroom.RoomId);

            var newRoomPublisher = await _publisherService.GetByIdAsync(newRoom.PublisherId);
            var publisher = await _publisherService.GetByIdAsync(classroom.PublisherId);
            var spacePublisher = await _publisherService.GetByIdAsync(_space.PublisherId);

            Assert.NotNull(publisher);
            _eventAssertionsBuilder.Build(changeEvent)
                .HasName("CLASSROOM_CHANGE_ROOM")
                .HasActor(_actor)
                .HasPublisher(publisher)
                .HasPublisher(spacePublisher)
                .HasPublisher(newRoomPublisher)
                .HasData(eventData);
        }
        
        [Test]
        public async Task ChangeClassroomRoom()
        {
            _model.RoomId = _room.Id;
            var result = await _classroomService.AddAsync(_space, _model, _adminUser);
            var classroom = result.Item;

            var newRoom = (await _roomService.AddRoomAsync(_space, 
                new RoomAddModel { Name = "room23", Capacity = 20}, _adminUser)).Item;
            
            var eventData = new ChangeValueData<ulong>(_room.Id, newRoom.Id);
            var changeEvent = await _classroomService.ChangeRoomAsync(classroom, newRoom, _adminUser);
            
            await _dbContext.Entry(classroom).ReloadAsync();
            
            Assert.AreEqual(newRoom.Id, classroom.RoomId);

            var roomPublisher = await _publisherService.GetByIdAsync(_room.PublisherId);
            var newRoomPublisher = await _publisherService.GetByIdAsync(newRoom.PublisherId);
            var publisher = await _publisherService.GetByIdAsync(classroom.PublisherId);
            var spacePublisher = await _publisherService.GetByIdAsync(_space.PublisherId);

            Assert.NotNull(publisher);
            _eventAssertionsBuilder.Build(changeEvent)
                .HasName("CLASSROOM_CHANGE_ROOM")
                .HasActor(_actor)
                .HasPublisher(publisher)
                .HasPublisher(spacePublisher)
                .HasPublisher(roomPublisher)
                .HasPublisher(newRoomPublisher)
                .HasData(eventData);
        }
        
        [Test]
        public async Task TryChangeClassroomNameWithUsedName_ShouldThrow()
        {
            var classroom = (await _classroomService.AddAsync(_space, _model, _adminUser)).Item;

            string newName = classroom.Name;
            
            var ex = Assert.ThrowsAsync<UsedValueException>(async () =>
            {
                await _classroomService.ChangeNameAsync(classroom, newName, _adminUser);
            });
            
            Assert.AreEqual("ClassroomNameUsed", ex!.Message);
        }
        
        


        [Test]
        public async Task DeleteClassroom()
        {
            var classroom = (await _classroomService.AddAsync(_space, _model, _adminUser)).Item;
            
            var deleteEvent = await _classroomService.DeleteAsync(classroom, _adminUser);
            await _dbContext.Entry(classroom).ReloadAsync();
            
            Assert.AreEqual("", classroom.Name);
            Assert.AreEqual("", classroom.NormalizedName);
            Assert.NotNull(classroom.DeletedAt);
            Assert.True(classroom.IsDeleted);

            var publisher = await _publisherService.GetByIdAsync(classroom.PublisherId);
            var spacePublisher = await _publisherService.GetByIdAsync(_space.PublisherId);

            _eventAssertionsBuilder.Build(deleteEvent)
                .HasName("CLASSROOM_DELETE")
                .HasActor(_actor)
                .HasPublisher(publisher)
                .HasPublisher(spacePublisher)
                .HasData(classroom);
        }


        [Test]
        public async Task IsSpaceClassroom()
        {
            await _classroomService.AddAsync(_space, _model, _adminUser);
            var isClassroom = await _classroomService.ContainsAsync(_space, _model.Name);
            Assert.True(isClassroom);
        }


        [Test]
        public async Task IsSpaceClassroom_WithNonClassroom_ShouldBeFalse()
        {
            var isClassroom = await _classroomService.ContainsAsync(_space, Guid.NewGuid().ToString());
            Assert.False(isClassroom);
        }

        [Test]
        public async Task IsClassroom_WithDeletedClassroom_ShouldBeFalse()
        {
            var classroom = (await _classroomService.AddAsync(_space, _model, _adminUser)).Item;
            await _classroomService.DeleteAsync(classroom, _adminUser);
            var isClassroom = await _classroomService.ContainsAsync(_space, _model.Name);
            Assert.False(isClassroom);
        }
        
        
        
        
        [Test]
        public async Task GetClassroom()
        {
            var classroom = (await _classroomService.AddAsync(_space, _model, _adminUser)).Item;
            var resultClassroom = await _classroomService.GetAsync(classroom.Id);
            Assert.AreEqual(classroom.Id, resultClassroom.Id);
        }


        [Test]
        public async Task GetNonExistingClassroom_ShouldThrow()
        {
            var ex = Assert.ThrowsAsync<ElementNotFoundException>(async () =>
            {
                await _classroomService.GetAsync(9000000000);
            });
            
            Assert.AreEqual("ClassroomNotFound", ex!.Message);
        }
        
        
        [Test]
        public async Task GetClassroomByName()
        {
            var classroom = (await _classroomService.AddAsync(_space, _model, _adminUser)).Item;
            var resultClassroom = await _classroomService.GetByNameAsync(_space, classroom.NormalizedName);
            Assert.AreEqual(classroom.Id, resultClassroom.Id);
        }


        [Test]
        public void GetNonExistingClassroomByName_ShouldThrow()
        {
            var ex = Assert.ThrowsAsync<ElementNotFoundException>(async () =>
            {
                await _classroomService.GetByNameAsync(_space, Guid.NewGuid().ToString());
            });
            
            Assert.AreEqual("ClassroomNotFoundByName", ex!.Message);
        }
        
    }
}