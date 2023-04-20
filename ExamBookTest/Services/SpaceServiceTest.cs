using System;
using System.Threading.Tasks;
using ExamBook.Exceptions;
using ExamBook.Helpers;
using ExamBook.Models;
using ExamBook.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace ExamBookTest.Services
{
    public class SpaceServiceTest
    {
        private IServiceProvider _provider = null!;
        private SpaceService _spaceService = null!;
        private DbContext _dbContext = null!;
        private string userId = Guid.NewGuid().ToString();

        private SpaceAddModel _model = new ()
        {
            Name = "UY-1, PHILOSPHIE, L1",
            Identifier = "uy1_phi_l1",
            IsPublic = true
        };
            
        
        [SetUp]
        public void Setup()
        {
            var services = new ServiceCollection();
            services.Setup();
            
            _provider = services.BuildServiceProvider();
            _spaceService = _provider.GetRequiredService<SpaceService>();
            _dbContext = _provider.GetRequiredService<DbContext>();
            _dbContext.Database.EnsureDeleted();

        }


        [Test]
        public async Task AddSpace()
        {
            var space = await _spaceService.AddAsync(userId, _model);
            await _dbContext.Entry(space).ReloadAsync();
            
            Assert.AreEqual(_model.Name, space.Name);
            Assert.AreEqual(_model.Identifier, space.Identifier);
            Assert.AreEqual(StringHelper.Normalize(_model.Identifier), space.NormalizedIdentifier);
            Assert.AreEqual(_model.IsPublic, space.IsPublic);
        }


        [Test]
        public async Task AddSpace_DuplicationIdentifier_ShouldThrow()
        {
            await _spaceService.AddAsync(userId, _model);

            var ex = Assert.ThrowsAsync<InvalidOperationException>(async () =>
            {
                await _spaceService.AddAsync(userId, _model);
            });
        }


        [Test]
        public async Task ChangeIdentifier()
        {
            var space = await _spaceService.AddAsync(userId, _model);
            const string identifier = "new_identifier";

            await _spaceService.ChangeIdentifier(space, identifier);
            await _dbContext.Entry(space).ReloadAsync();
            
            Assert.AreEqual(identifier, space.Identifier);
            Assert.AreEqual(StringHelper.Normalize(identifier), space.NormalizedIdentifier);
        }
        
        [Test]
        public async Task ChangeIdentifier_WithUsedIdentifier_ShouldThrow()
        {
            var space = await _spaceService.AddAsync(userId, _model);
            var identifier = "new_identifier";
            await _spaceService.ChangeIdentifier(space, identifier);
            await _dbContext.Entry(space).ReloadAsync();
            
            var ex = Assert.ThrowsAsync<InvalidOperationException>(async () =>
            {
                await _spaceService.ChangeIdentifier(space, identifier);
            });
        }

        [Test]
        public async Task SetAsPrivate()
        {
            _model.IsPublic = true;
            var space = await _spaceService.AddAsync(userId, _model);
            Assert.True(space.IsPublic);

            await _spaceService.SetAsPrivate(space);
            await _dbContext.Entry(space).ReloadAsync();
            
            Assert.False(space.IsPublic);
        }
        
        [Test]
        public async Task SetAsPrivate_WithPrivateSpace_ShouldThrow()
        {
            _model.IsPublic = false;
            var space = await _spaceService.AddAsync(userId, _model);

            var ex = Assert.ThrowsAsync<IllegalOperationException>(async () =>
            {
                await _spaceService.SetAsPrivate(space);
            });

            await _dbContext.Entry(space).ReloadAsync();
            Assert.False(space.IsPublic);
            Assert.AreEqual(ex!.Message, "SpaceIsNotPublic");
        }

        [Test]
        public async Task SetAsPublic()
        {
            _model.IsPublic = false;
            var space = await _spaceService.AddAsync(userId, _model);

            await _spaceService.SetAsPublic(space);
            await _dbContext.Entry(space).ReloadAsync();
            
            Assert.True(space.IsPublic);
        }
        
        
        [Test]
        public async Task SetAsPublic_WithPublicSpace_ShouldThrow()
        {
            _model.IsPublic = true;
            var space = await _spaceService.AddAsync(userId, _model);

            var ex = Assert.ThrowsAsync<IllegalOperationException>(async () =>
            {
                await _spaceService.SetAsPublic(space);
            });

            await _dbContext.Entry(space).ReloadAsync();
            Assert.False(space.IsPublic);
            Assert.AreEqual(ex!.Message, "SpaceIsNotPrivate");
        }
    }
}