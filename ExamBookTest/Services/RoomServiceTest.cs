using System;
using System.Threading.Tasks;
using ExamBook.Entities;
using ExamBook.Exceptions;
using ExamBook.Helpers;
using ExamBook.Identity;
using ExamBook.Identity.Entities;
using ExamBook.Identity.Models;
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
    public class RoomServiceTest
    {
        private IServiceProvider _provider = null!;
        private RoomService _roomService = null!;
        private SpaceService _spaceService = null!;
        private PublisherService _publisherService = null!;
        private EventAssertionsBuilder _eventAssertionsBuilder = null!;
        
        private DbContext _dbContext = null!;
        private User _adminUser = null!;
        private Actor _actor = null!;

        private Space _space = null!;
        private RoomAddModel _model = null!;
            
        
        [SetUp]
        public async Task Setup()
        {
            var services = new ServiceCollection();
            
            _provider = services.Setup();
            _roomService = _provider.GetRequiredService<RoomService>();
            _publisherService = _provider.GetRequiredService<PublisherService>();
            _eventAssertionsBuilder = _provider.GetRequiredService<EventAssertionsBuilder>();
            _dbContext = _provider.GetRequiredService<DbContext>();

            var userService = _provider.GetRequiredService<UserService>();
            _spaceService = _provider.GetRequiredService<SpaceService>();
            _adminUser = await userService.AddUserAsync(ServiceExtensions.UserAddModel);
            _actor = await userService.GetActor(_adminUser);

            var result = await _spaceService.AddAsync(_adminUser.Id, new SpaceAddModel {
                Name = "UY-1, PHILOSOPHY, L1",
                Identifier = "uy1_phi_l1"
            });
            _space = result.Item;

            _model = new RoomAddModel
            {
                Name = "Room name",
                Capacity = 10
            };
        }


        [Test]
        public async Task AddRoom()
        {
            var result = await _roomService.AddAsync(_space, _model, _adminUser);
            var room = result.Item;
            
            await _dbContext.Entry(room).ReloadAsync();
            
            Assert.AreEqual(_model.Name, room.Name);
            Assert.AreEqual(StringHelper.Normalize(_model.Name), room.NormalizedName);
            Assert.AreEqual(_model.Capacity, room.Capacity);
            Assert.AreEqual(_space.Id, room.SpaceId);

            var publisher = await _publisherService.GetByIdAsync(room.PublisherId);
            var spacePublisher = await _publisherService.GetByIdAsync(_space.PublisherId);
            var addEvent = result.Event;

            Assert.NotNull(publisher);
            _eventAssertionsBuilder.Build(addEvent)
                .HasName("ROOM_ADD")
                .HasActor(_actor)
                .HasPublisher(publisher)
                .HasPublisher(spacePublisher)
                .HasData(room);
        }

        
        [Test]
        public async Task TryAddRoomWithUsedName_ShouldThrow()
        {
            await _roomService.AddAsync(_space, _model, _adminUser);

            var ex = Assert.ThrowsAsync<UsedValueException>(async () =>
            {
                await _roomService.AddAsync(_space, _model, _adminUser);
            });
            
            Assert.AreEqual("RoomNameUsed", ex!.Message);
        }
        
        [Test]
        public async Task ChangeRoomName()
        {
            var result = await _roomService.AddAsync(_space, _model, _adminUser);
            var room = result.Item;

            var changeNameModel = new RoomChangeNameModel
            {
                Name = "new room name"
            };
            var eventData = new ChangeValueData<string>(room.Name, changeNameModel.Name);
            var changeEvent = await _roomService.ChangeNameAsync(room, changeNameModel, _adminUser);
            
            await _dbContext.Entry(room).ReloadAsync();
            
            Assert.AreEqual(changeNameModel.Name, room.Name);
            Assert.AreEqual(StringHelper.Normalize(changeNameModel.Name), room.NormalizedName);

            var publisher = await _publisherService.GetByIdAsync(room.PublisherId);
            var spacePublisher = await _publisherService.GetByIdAsync(_space.PublisherId);

            Assert.NotNull(publisher);
            _eventAssertionsBuilder.Build(changeEvent)
                .HasName("ROOM_CHANGE_NAME")
                .HasActor(_actor)
                .HasPublisher(publisher)
                .HasPublisher(spacePublisher)
                .HasData(eventData);
        }
        
        [Test]
        public async Task TryChangeRoomNameWithUsedName_ShouldThrow()
        {
            var room = (await _roomService.AddAsync(_space, _model, _adminUser)).Item;

            var changeNameModel = new RoomChangeNameModel
            {
                Name = _model.Name
            };
            
            var ex = Assert.ThrowsAsync<UsedValueException>(async () =>
            {
                await _roomService.ChangeNameAsync(room, changeNameModel, _adminUser);
            });
            
            Assert.AreEqual("RoomNameUsed", ex!.Message);
        }
        
        
        
        [Test]
        public async Task ChangeRoomCapacity()
        {
            var result = await _roomService.AddAsync(_space, _model, _adminUser);
            var room = result.Item;

            var changeCapacityModel = new RoomChangeCapacityModel { Capacity = 100 };
            var eventData = new ChangeValueData<uint>(room.Capacity, changeCapacityModel.Capacity);
            
            var changeEvent = await _roomService.ChangeCapacityAsync(room, changeCapacityModel, _adminUser);
            await _dbContext.Entry(room).ReloadAsync();
            
            Assert.AreEqual(changeCapacityModel.Capacity, room.Capacity);

            var publisher = await _publisherService.GetByIdAsync(room.PublisherId);
            var spacePublisher = await _publisherService.GetByIdAsync(_space.PublisherId);

            Assert.NotNull(publisher);
            _eventAssertionsBuilder.Build(changeEvent)
                .HasName("ROOM_CHANGE_CAPACITY")
                .HasActor(_actor)
                .HasPublisher(publisher)
                .HasPublisher(spacePublisher)
                .HasData(eventData);
        }


        [Test]
        public async Task DeleteRoom()
        {
            var room = (await _roomService.AddAsync(_space, _model, _adminUser)).Item;
            
            var deleteEvent = await _roomService.DeleteAsync(room, _adminUser);
            await _dbContext.Entry(room).ReloadAsync();
            
            Assert.IsEmpty(room.Name);
            Assert.IsEmpty(room.NormalizedName);
            Assert.Zero(room.Capacity);
            Assert.NotNull(room.DeletedAt);
            Assert.True(room.IsDeleted);

            var publisher = await _publisherService.GetByIdAsync(room.PublisherId);
            var spacePublisher = await _publisherService.GetByIdAsync(_space.PublisherId);

            _eventAssertionsBuilder.Build(deleteEvent)
                .HasName("ROOM_DELETE")
                .HasActor(_actor)
                .HasPublisher(publisher)
                .HasPublisher(spacePublisher)
                .HasData(room);
        }


        [Test]
        public async Task IsSpaceRoom()
        {
            await _roomService.AddAsync(_space, _model, _adminUser);
            var isRoom = await _roomService.ContainsAsync(_space, _model.Name);
            Assert.True(isRoom);
        }


        [Test]
        public async Task IsSpaceRoom_WithNonRoom_ShouldBeFalse()
        {
            var isRoom = await _roomService.ContainsAsync(_space, Guid.NewGuid().ToString());
            Assert.False(isRoom);
        }

        [Test]
        public async Task IsRoom_WithDeletedUser_ShouldBeFalse()
        {
            var room = (await _roomService.AddAsync(_space, _model, _adminUser)).Item;
            await _roomService.DeleteAsync(room, _adminUser);
            var isRoom = await _roomService.ContainsAsync(_space, _model.Name);
            Assert.False(isRoom);
        }
        
        
        [Test]
        public async Task GetRoom()
        {
            var room = (await _roomService.AddAsync(_space, _model, _adminUser)).Item;
            var resultRoom = await _roomService.GetRoomAsync(room.Id);
            Assert.AreEqual(room.Id, resultRoom.Id);
        }


        [Test]
        public void GetNonExistingRoom_ShouldThrow()
        {
            var ex = Assert.ThrowsAsync<ElementNotFoundException>(async () =>
            {
                await _roomService.GetRoomAsync(9000000000);
            });
            
            Assert.AreEqual("RoomNotFound", ex!.Message);
        }
        
        
        [Test]
        public async Task GetRoomByName()
        {
            var room = (await _roomService.AddAsync(_space, _model, _adminUser)).Item;
            var resultRoom = await _roomService.GetByNameAsync(_space, room.NormalizedName);
            Assert.AreEqual(room.Id, resultRoom.Id);
        }


        [Test]
        public void GetNonExistingRoomByName_ShouldThrow()
        {
            var ex = Assert.ThrowsAsync<ElementNotFoundException>(async () =>
            {
                await _roomService.GetByNameAsync(_space, Guid.NewGuid().ToString());
            });
            
            Assert.AreEqual("RoomNotFoundByName", ex!.Message);
        }
      
    }
}