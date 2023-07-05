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
using Vx.Asserts;
using Vx.Models;
using Vx.Services;

#pragma warning disable NUnit2005
namespace ExamBookTest.Services
{
    public class ExaminationServiceTest
    {
        private IServiceProvider _provider = null!;
        private ExaminationService _examinationService = null!;
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
        private ExaminationAddModel _model = null!;
            
        
        [SetUp]
        public async Task Setup()
        {
            var services = new ServiceCollection();
            
            _provider = services.Setup();
            _examinationService = _provider.GetRequiredService<ExaminationService>();
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

            _model = new ExaminationAddModel
            {
                Name = "Examination name",
                StartAt = DateTime.Now.AddDays(-12)
            };
        }


        [Test]
        public async Task AddExamination()
        {
            var result = await _examinationService.AddAsync(_space, _model, _adminUser);
            var examination = result.Item;
            
            await _dbContext.Entry(examination).ReloadAsync();
            
            Assert.AreEqual(_model.Name, examination.Name);
            Assert.AreEqual(StringHelper.Normalize(_model.Name), examination.NormalizedName);
            Assert.AreEqual(_space.Id, examination.SpaceId);

            var publisher = await _publisherService.GetByIdAsync(examination.PublisherId);
            var spacePublisher = await _publisherService.GetByIdAsync(_space.PublisherId);
            var addEvent = result.Event;

            Assert.NotNull(publisher);
            _eventAssertionsBuilder.Build(addEvent)
                .HasName("EXAMINATION_ADD")
                .HasActor(_actor)
                .HasPublisher(publisher)
                .HasPublisher(spacePublisher)
                .HasData(examination);
        }
        
        
        
        [Test]
        public async Task TryAddExaminationWithUsedName_ShouldThrow()
        {
            await _examinationService.AddAsync(_space, _model, _adminUser);

            var ex = Assert.ThrowsAsync<UsedValueException>(async () =>
            {
                await _examinationService.AddAsync(_space, _model, _adminUser);
            });
            
            Assert.AreEqual("ExaminationNameUsed{0}", ex!.Message);
            Assert.AreEqual(_model.Name, ex.Params[0]);
        }
        
        
        [Test]
        public async Task ChangeExaminationName()
        {
            var result = await _examinationService.AddAsync(_space, _model, _adminUser);
            var examination = result.Item;

            var newName = "new examination name";
            var eventData = new ChangeValueData<string>(examination.Name, newName);
            var changeEvent = await _examinationService.ChangeNameAsync(examination, newName, _adminUser);
            
            await _dbContext.Entry(examination).ReloadAsync();
            
            Assert.AreEqual(newName, examination.Name);
            Assert.AreEqual(StringHelper.Normalize(newName), examination.NormalizedName);

            var publisher = await _publisherService.GetByIdAsync(examination.PublisherId);
            var spacePublisher = await _publisherService.GetByIdAsync(_space.PublisherId);

            Assert.NotNull(publisher);
            _eventAssertionsBuilder.Build(changeEvent)
                .HasName("EXAMINATION_CHANGE_NAME")
                .HasActor(_actor)
                .HasPublisher(publisher)
                .HasPublisher(spacePublisher)
                .HasData(eventData);
        }
        
       
        [Test]
        public async Task TryChangeExaminationNameWithUsedName_ShouldThrow()
        {
            var examination = (await _examinationService.AddAsync(_space, _model, _adminUser)).Item;

            string newName = examination.Name;
            
            var ex = Assert.ThrowsAsync<UsedValueException>(async () =>
            {
                await _examinationService.ChangeNameAsync(examination, newName, _adminUser);
            });
            
            Assert.AreEqual("ExaminationNameUsed{0}", ex!.Message);
            Assert.AreEqual(_model.Name, ex.Params[0]);
        }
        
        
        [Test]
        public async Task ChangeExaminationStartAt()
        {
            var result = await _examinationService.AddAsync(_space, _model, _adminUser);
            var examination = result.Item;

            var newDate = DateTime.Now.AddDays(-5);
            var eventData = new ChangeValueData<DateTime>(examination.StartAt, newDate);
            var changeEvent = await _examinationService.ChangeStartAtAsync(examination, newDate, _adminUser);
            
            await _dbContext.Entry(examination).ReloadAsync();
            
            Assert.AreEqual(newDate, examination.StartAt);

            var publisher = await _publisherService.GetByIdAsync(examination.PublisherId);
            var spacePublisher = await _publisherService.GetByIdAsync(_space.PublisherId);

            Assert.NotNull(publisher);
            _eventAssertionsBuilder.Build(changeEvent)
                .HasName("EXAMINATION_CHANGE_START_AT")
                .HasActor(_actor)
                .HasPublisher(publisher)
                .HasPublisher(spacePublisher)
                .HasData(eventData);
        }
        
        [Test]
        public async Task TryChangeExaminationStartAtAfterNow_ShouldThrow()
        {
            var examination = (await _examinationService.AddAsync(_space, _model, _adminUser)).Item;

            var newDate = DateTime.Now.AddDays(5);
            var ex = Assert.ThrowsAsync<IllegalValueException>(async () =>
            {
                await _examinationService.ChangeStartAtAsync(examination, newDate, _adminUser);
            });
            
            Assert.AreEqual("StartDateBeforeNow", ex!.Message);
        }
        
        
        [Test]
        public async Task ExaminationLock()
        {
            var result = await _examinationService.AddAsync(_space, _model, _adminUser);
            var examination = result.Item;

            var changeEvent = await _examinationService.LockAsync(examination, _adminUser);
            
            await _dbContext.Entry(examination).ReloadAsync();
            
            Assert.AreEqual(true, examination.IsLock);

            var publisher = await _publisherService.GetByIdAsync(examination.PublisherId);
            var spacePublisher = await _publisherService.GetByIdAsync(_space.PublisherId);

            Assert.NotNull(publisher);
            _eventAssertionsBuilder.Build(changeEvent)
                .HasName("EXAMINATION_LOCK")
                .HasActor(_actor)
                .HasPublisher(publisher)
                .HasPublisher(spacePublisher)
                .HasData(new {});
        }


        [Test]
        public async Task TryLock_LockedExamination_ShouldThrow()
        {
            var examination = (await _examinationService.AddAsync(_space, _model, _adminUser)).Item;
            await _examinationService.LockAsync(examination, _adminUser);

            var ex = Assert.ThrowsAsync<IllegalStateException>(async () =>
            {
                await _examinationService.LockAsync(examination, _adminUser);
            });
            Assert.AreEqual("ExaminationIsLocked", ex!.Message);
        }


        
        public async Task ExaminationUnLock()
        {
            var result = await _examinationService.AddAsync(_space, _model, _adminUser);
            var examination = result.Item;

            await _examinationService.LockAsync(examination, _adminUser);
            var changeEvent = await _examinationService.UnLockAsync(examination, _adminUser);
            
            await _dbContext.Entry(examination).ReloadAsync();
            
            Assert.AreEqual(false, examination.IsLock);

            var publisher = await _publisherService.GetByIdAsync(examination.PublisherId);
            var spacePublisher = await _publisherService.GetByIdAsync(_space.PublisherId);

            Assert.NotNull(publisher);
            _eventAssertionsBuilder.Build(changeEvent)
                .HasName("EXAMINATION_UNLOCK")
                .HasActor(_actor)
                .HasPublisher(publisher)
                .HasPublisher(spacePublisher)
                .HasData(new {});
        }


        [Test]
        public async Task TryUnLock_UnLockedExamination_ShouldThrow()
        {
            var examination = (await _examinationService.AddAsync(_space, _model, _adminUser)).Item;

            var ex = Assert.ThrowsAsync<IllegalStateException>(async () =>
            {
                await _examinationService.UnLockAsync(examination, _adminUser);
            });
            Assert.AreEqual("ExaminationIsNotLocked", ex!.Message);
        }
        
        
        // [Test]
        // public async Task DeleteExamination()
        // {
        //     // var examination = (await _examinationService.AddAsync(_space, _model, _adminUser)).Item;
        //     //
        //     // var deleteEvent = await _examinationService.DeleteAsync(examination, _adminUser);
        //     // await _dbContext.Entry(examination).ReloadAsync();
        //     //
        //     // Assert.AreEqual("", examination.Name);
        //     // Assert.AreEqual("", examination.NormalizedName);
        //     // Assert.NotNull(examination.DeletedAt);
        //     // Assert.True(examination.IsDeleted);
        //     //
        //     // var publisher = await _publisherService.GetByIdAsync(examination.PublisherId);
        //     // var spacePublisher = await _publisherService.GetByIdAsync(_space.PublisherId);
        //     //
        //     // _eventAssertionsBuilder.Build(deleteEvent)
        //     //     .HasName("EXAMINATION_DELETE")
        //     //     .HasActor(_actor)
        //     //     .HasPublisher(publisher)
        //     //     .HasPublisher(spacePublisher)
        //     //     .HasData(examination);
        // }


        [Test]
        public async Task ContainsExamination()
        {
            await _examinationService.AddAsync(_space, _model, _adminUser);
            var isExamination = await _examinationService.ContainsAsync(_space, _model.Name);
            Assert.True(isExamination);
        }


        [Test]
        public async Task Contains_WithNonExamination_ShouldBeFalse()
        {
            var isExamination = await _examinationService.ContainsAsync(_space, Guid.NewGuid().ToString());
            Assert.False(isExamination);
        }

        // [Test]
        // public async Task IsExamination_WithDeletedExamination_ShouldBeFalse()
        // {
        //     var examination = (await _examinationService.AddAsync(_space, _model, _adminUser)).Item;
        //     await _examinationService.DeleteAsync(examination, _adminUser);
        //     var isExamination = await _examinationService.ContainsAsync(_space, _model.Name);
        //     Assert.False(isExamination);
        // }
        
        
        
        
        [Test]
        public async Task GetExamination()
        {
            var examination = (await _examinationService.AddAsync(_space, _model, _adminUser)).Item;
            var resultExamination = await _examinationService.GetByIdAsync(examination.Id);
            Assert.AreEqual(examination.Id, resultExamination.Id);
        }


        [Test]
        public void GetNonExistingExamination_ShouldThrow()
        {
            var ex = Assert.ThrowsAsync<ElementNotFoundException>(async () =>
            {
                await _examinationService.GetByIdAsync(9000000000);
            });
            
            Assert.AreEqual("ExaminationNotFoundById", ex!.Message);
        }
        
        
        [Test]
        public async Task GetExaminationByName()
        {
            var examination = (await _examinationService.AddAsync(_space, _model, _adminUser)).Item;
            var resultExamination = await _examinationService.GetByNameAsync(examination.NormalizedName);
            Assert.AreEqual(examination.Id, resultExamination.Id);
        }


        [Test]
        public void GetNonExistingExaminationByName_ShouldThrow()
        {
            var ex = Assert.ThrowsAsync<ElementNotFoundException>(async () =>
            {
                await _examinationService.GetByNameAsync(Guid.NewGuid().ToString());
            });
            
            Assert.AreEqual("ExaminationNotFoundByName{0}", ex!.Message);
        }
        
    }
}