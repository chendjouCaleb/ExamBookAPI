using System;
using System.Threading.Tasks;
using ExamBook.Entities;
using ExamBook.Exceptions;
using ExamBook.Helpers;
using ExamBook.Identity.Entities;
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
    public class SpecialityServiceTest
    {
        private IServiceProvider _provider = null!;
        private SpecialityService _specialityService = null!;
        private SpaceService _spaceService = null!;
        private PublisherService _publisherService = null!;
        private EventAssertionsBuilder _eventAssertionsBuilder = null!;
        
        private DbContext _dbContext = null!;
        private User _adminUser = null!;
        private Actor _actor = null!;

        private Space _space = null!;
        private SpecialityAddModel _model = null!;
            
        
        [SetUp]
        public async Task Setup()
        {
            var services = new ServiceCollection();
            
            _provider = services.Setup();
            _specialityService = _provider.GetRequiredService<SpecialityService>();
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

            _model = new SpecialityAddModel
            {
                Name = "Speciality name"
            };
        }


        [Test]
        public async Task AddSpeciality()
        {
            var result = await _specialityService.AddSpecialityAsync(_space, _model, _adminUser);
            var speciality = result.Item;
            
            await _dbContext.Entry(speciality).ReloadAsync();
            
            Assert.AreEqual(_model.Name, speciality.Name);
            Assert.AreEqual(StringHelper.Normalize(_model.Name), speciality.NormalizedName);
            Assert.AreEqual(_space.Id, speciality.SpaceId);

            var publisher = await _publisherService.GetByIdAsync(speciality.PublisherId);
            var spacePublisher = await _publisherService.GetByIdAsync(_space.PublisherId);
            var addEvent = result.Event;

            Assert.NotNull(publisher);
            _eventAssertionsBuilder.Build(addEvent)
                .HasName("SPECIALITY_ADD")
                .HasActor(_actor)
                .HasPublisher(publisher)
                .HasPublisher(spacePublisher)
                .HasData(speciality);
        }

        
        [Test]
        public async Task TryAddSpecialityWithUsedName_ShouldThrow()
        {
            await _specialityService.AddSpecialityAsync(_space, _model, _adminUser);

            var ex = Assert.ThrowsAsync<UsedValueException>(async () =>
            {
                await _specialityService.AddSpecialityAsync(_space, _model, _adminUser);
            });
            
            Assert.AreEqual("SpecialityNameUsed", ex!.Message);
        }
        
        [Test]
        public async Task ChangeSpecialityName()
        {
            var result = await _specialityService.AddSpecialityAsync(_space, _model, _adminUser);
            var speciality = result.Item;

            var newName = "new speciality name";
            var eventData = new ChangeValueData<string>(speciality.Name, newName);
            var changeEvent = await _specialityService.ChangeNameAsync(speciality, newName, _adminUser);
            
            await _dbContext.Entry(speciality).ReloadAsync();
            
            Assert.AreEqual(newName, speciality.Name);
            Assert.AreEqual(StringHelper.Normalize(newName), speciality.NormalizedName);

            var publisher = await _publisherService.GetByIdAsync(speciality.PublisherId);
            var spacePublisher = await _publisherService.GetByIdAsync(_space.PublisherId);

            Assert.NotNull(publisher);
            _eventAssertionsBuilder.Build(changeEvent)
                .HasName("SPECIALITY_CHANGE_NAME")
                .HasActor(_actor)
                .HasPublisher(publisher)
                .HasPublisher(spacePublisher)
                .HasData(eventData);
        }
        
        [Test]
        public async Task TryChangeSpecialityNameWithUsedName_ShouldThrow()
        {
            var speciality = (await _specialityService.AddSpecialityAsync(_space, _model, _adminUser)).Item;

            string newName = speciality.Name;
            
            var ex = Assert.ThrowsAsync<UsedValueException>(async () =>
            {
                await _specialityService.ChangeNameAsync(speciality, newName, _adminUser);
            });
            
            Assert.AreEqual("SpecialityNameUsed", ex!.Message);
        }
        
        


        [Test]
        public async Task DeleteSpeciality()
        {
            var speciality = (await _specialityService.AddSpecialityAsync(_space, _model, _adminUser)).Item;
            
            var deleteEvent = await _specialityService.DeleteAsync(speciality, _adminUser);
            await _dbContext.Entry(speciality).ReloadAsync();
            
            Assert.AreEqual("", speciality.Name);
            Assert.AreEqual("", speciality.NormalizedName);
            Assert.NotNull(speciality.DeletedAt);
            Assert.True(speciality.IsDeleted);

            var publisher = await _publisherService.GetByIdAsync(speciality.PublisherId);
            var spacePublisher = await _publisherService.GetByIdAsync(_space.PublisherId);

            _eventAssertionsBuilder.Build(deleteEvent)
                .HasName("SPECIALITY_DELETE")
                .HasActor(_actor)
                .HasPublisher(publisher)
                .HasPublisher(spacePublisher)
                .HasData(speciality);
        }


        [Test]
        public async Task IsSpaceSpeciality()
        {
            await _specialityService.AddSpecialityAsync(_space, _model, _adminUser);
            var isSpeciality = await _specialityService.ContainsAsync(_space, _model.Name);
            Assert.True(isSpeciality);
        }


        [Test]
        public async Task IsSpaceSpeciality_WithNonSpeciality_ShouldBeFalse()
        {
            var isSpeciality = await _specialityService.ContainsAsync(_space, Guid.NewGuid().ToString());
            Assert.False(isSpeciality);
        }

        [Test]
        public async Task IsSpeciality_WithDeletedUser_ShouldBeFalse()
        {
            var speciality = (await _specialityService.AddSpecialityAsync(_space, _model, _adminUser)).Item;
            await _specialityService.DeleteAsync(speciality, _adminUser);
            var isSpeciality = await _specialityService.ContainsAsync(_space, _model.Name);
            Assert.False(isSpeciality);
        }
        
        
        
        
        [Test]
        public async Task GetSpeciality()
        {
            var speciality = (await _specialityService.AddSpecialityAsync(_space, _model, _adminUser)).Item;
            var resultSpeciality = await _specialityService.GetAsync(speciality.Id);
            Assert.AreEqual(speciality.Id, resultSpeciality.Id);
        }


        [Test]
        public void GetNonExistingSpeciality_ShouldThrow()
        {
            var ex = Assert.ThrowsAsync<ElementNotFoundException>(async () =>
            {
                await _specialityService.GetAsync(9000000000);
            });
            
            Assert.AreEqual("SpecialityNotFound", ex!.Message);
        }
        
        
        [Test]
        public async Task GetSpecialityByName()
        {
            var speciality = (await _specialityService.AddSpecialityAsync(_space, _model, _adminUser)).Item;
            var resultSpeciality = await _specialityService.GetByNameAsync(_space, speciality.NormalizedName);
            Assert.AreEqual(speciality.Id, resultSpeciality.Id);
        }


        [Test]
        public void GetNonExistingSpecialityByName_ShouldThrow()
        {
            var ex = Assert.ThrowsAsync<ElementNotFoundException>(async () =>
            {
                await _specialityService.GetByNameAsync(_space, Guid.NewGuid().ToString());
            });
            
            Assert.AreEqual("SpecialityNotFoundByName", ex!.Message);
        }
      
    }
}