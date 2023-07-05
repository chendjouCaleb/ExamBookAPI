using System;
using System.Threading.Tasks;
using ExamBook.Exceptions;
using ExamBook.Helpers;
using ExamBook.Identity.Entities;
using ExamBook.Identity.Services;
using ExamBook.Models;
using ExamBook.Models.Data;
using ExamBook.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Vx.Asserts;
using Vx.Models;

#pragma warning disable NUnit2005
namespace ExamBookTest.Services
{
    public class SpaceServiceTest
    {
        private IServiceProvider _provider = null!;
        private SpaceService _spaceService = null!;
        private EventAssertionsBuilder _eventAssertionsBuilder = null!;
        
        private DbContext _dbContext = null!;
        private User _user = null!;
        private Actor _actor = null!;
        private string _userId = null!;

        private SpaceAddModel _model = new ()
        {
            Name = "UY-1, PHILOSPHIE, L1",
            Identifier = "uy1_phi_l1",
            IsPublic = true
        };
            
        
        [SetUp]
        public async Task Setup()
        {
            var services = new ServiceCollection();
            
            _provider = services.Setup();
            _spaceService = _provider.GetRequiredService<SpaceService>();
            _eventAssertionsBuilder = _provider.GetRequiredService<EventAssertionsBuilder>();
            _dbContext = _provider.GetRequiredService<DbContext>();

            var userService = _provider.GetRequiredService<UserService>();
            _user = await userService.AddUserAsync(ServiceExtensions.UserAddModel);
            _userId = (await userService.FindByIdAsync(_user.Id)).Id;
            _actor = await userService.GetActor(_user);
        }


        [Test]
        public async Task AddSpace()
        {
            var result = await _spaceService.AddAsync(_userId, _model);
            var space = result.Item;
            
            await _dbContext.Entry(space).ReloadAsync();
            
            Assert.AreEqual(_model.Name, space.Name);
            Assert.AreEqual(_model.Identifier, space.Identifier);
            Assert.AreEqual(StringHelper.Normalize(_model.Identifier), space.NormalizedIdentifier);
            Assert.AreEqual(_model.IsPublic, space.IsPublic);

            var publisher = await _spaceService.GetPublisherAsync(space);
            
            var addEvent = result.Event;

            _eventAssertionsBuilder.Build(addEvent)
                .HasName("SPACE_ADD")
                .HasActor(_actor)
                .HasPublisher(publisher)
                .HasData(space);
        }


        [Test]
        public async Task AddSpace_DuplicationIdentifier_ShouldThrow()
        {
            await _spaceService.AddAsync(_user.Id, _model);

            var ex = Assert.ThrowsAsync<UsedValueException>(async () =>
            {
                await _spaceService.AddAsync(_user.Id, _model);
            });
            
            Assert.AreEqual("SpaceIdentifierUsed", ex!.Message);
        }


        [Test]
        public async Task ChangeIdentifier()
        {
            var space = (await _spaceService.AddAsync(_user.Id, _model)).Item;
            var publisher = await _spaceService.GetPublisherAsync(space);
            
            const string identifier = "new_identifier";
            var data = new ChangeValueData<string>(space.Identifier, identifier);

            var @event = await _spaceService.ChangeIdentifier(space, identifier, _user);
            await _dbContext.Entry(space).ReloadAsync();
            
            Assert.AreEqual(identifier, space.Identifier);
            Assert.AreEqual(StringHelper.Normalize(identifier), space.NormalizedIdentifier);

            _eventAssertionsBuilder.Build(@event)
                .HasName("SPACE_CHANGE_IDENTIFIER")
                .HasActor(_actor)
                .HasPublisher(publisher)
                .HasData(data);
        }
        
        
        [Test]
        public async Task ChangeName()
        {
            var space = (await _spaceService.AddAsync(_user.Id, _model)).Item;
            var publisher = await _spaceService.GetPublisherAsync(space);
            
            const string name = "new_name";
            var data = new ChangeValueData<string>(space.Name, name);

            var @event = await _spaceService.ChangeName(space, name, _user);
            await _dbContext.Entry(space).ReloadAsync();
            
            Assert.AreEqual(name, space.Name);

            _eventAssertionsBuilder.Build(@event)
                .HasName("SPACE_CHANGE_NAME")
                .HasActor(_actor)
                .HasPublisher(publisher)
                .HasData(data);
        }
        
        [Test]
        public async Task ChangeIdentifier_WithUsedIdentifier_ShouldThrow()
        {
            var space = (await _spaceService.AddAsync(_userId, _model)).Item;
            var identifier = "new_identifier";
            await _spaceService.ChangeIdentifier(space, identifier, _user);
            await _dbContext.Entry(space).ReloadAsync();
            
            var ex = Assert.ThrowsAsync<UsedValueException>(async () =>
            {
                await _spaceService.ChangeIdentifier(space, identifier, _user);
            });
            
            Assert.AreEqual("SpaceIdentifierUsed", ex!.Message);
        }

        [Test]
        public async Task SetAsPrivate()
        {
            _model.IsPublic = true;
            var space = (await _spaceService.AddAsync(_userId, _model)).Item;
            var publisher = await _spaceService.GetPublisherAsync(space);
            Assert.True(space.IsPublic);

            var @event = await _spaceService.SetAsPrivate(space, _user);
            await _dbContext.Entry(space).ReloadAsync();
            
            Assert.False(space.IsPublic);
            
            _eventAssertionsBuilder.Build(@event)
                .HasName("SPACE_AS_PRIVATE")
                .HasActor(_actor)
                .HasPublisher(publisher)
                .HasData(new {});
        }
        
        [Test]
        public async Task SetAsPrivate_WithPrivateSpace_ShouldThrow()
        {
            _model.IsPublic = false;
            var space = (await _spaceService.AddAsync(_userId, _model)).Item;

            var ex = Assert.ThrowsAsync<IllegalOperationException>(async () =>
            {
                await _spaceService.SetAsPrivate(space, _user);
            });

            await _dbContext.Entry(space).ReloadAsync();
            Assert.False(space.IsPublic);
            Assert.AreEqual( "SpaceIsNotPublic", ex!.Message);
        }

        [Test]
        public async Task SetAsPublic()
        {
            _model.IsPublic = false;
            var space = (await _spaceService.AddAsync(_userId, _model)).Item;
            var publisher = await _spaceService.GetPublisherAsync(space);

            var @event = await _spaceService.SetAsPublic(space, _user);
            await _dbContext.Entry(space).ReloadAsync();
            
            Assert.True(space.IsPublic);
            
            _eventAssertionsBuilder.Build(@event)
                .HasName("SPACE_AS_PUBLIC")
                .HasActor(_actor)
                .HasPublisher(publisher)
                .HasData(new {});
        }
        
        
        [Test]
        public async Task SetAsPublic_WithPublicSpace_ShouldThrow()
        {
            _model.IsPublic = true;
            var space = (await _spaceService.AddAsync(_userId, _model)).Item;

            var ex = Assert.ThrowsAsync<IllegalOperationException>(async () =>
            {
                await _spaceService.SetAsPublic(space, _user);
            });

            await _dbContext.Entry(space).ReloadAsync();
            Assert.True(space.IsPublic);
            Assert.AreEqual( "SpaceIsNotPrivate", ex!.Message);
        }
    }
}