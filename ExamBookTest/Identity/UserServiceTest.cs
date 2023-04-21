using System;
using System.Threading.Tasks;
using ExamBook.Identity;
using ExamBook.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Social.Services;
using Vx.Services;

#pragma warning disable NUnit2005
namespace ExamBookTest.Identity
{
    public class UserServiceTest
    {
        private IServiceProvider _provider = null!;
        private UserService _userService = null!;
        private AuthorService _authorService = null!;
        private ActorService _actorService = null!;
        private PublisherService _publisherService = null!;
        private DbContext _dbContext = null!;
        private ApplicationIdentityDbContext _identityDbContext = null!;
        private UserAddModel _model = new ()
        {
            FirstName = "first name",
            LastName = "last name",
            Sex = 'M',
            BirthDate = new DateTime(1995,1,1),
            UserName = "userName",
            Password = "Password09@"
        };
            
        
        [SetUp]
        public void Setup()
        {
            var services = new ServiceCollection();
            
            _provider = services.Setup();
            _userService = _provider.GetRequiredService<UserService>();
            _dbContext = _provider.GetRequiredService<DbContext>();
            _identityDbContext = _provider.GetRequiredService<ApplicationIdentityDbContext>();
            _authorService = _provider.GetRequiredService<AuthorService>();
            _actorService = _provider.GetRequiredService<ActorService>();
            _publisherService = _provider.GetRequiredService<PublisherService>();

            _identityDbContext.Database.EnsureDeleted();
            //_dbContext.Database.EnsureDeleted();
        }


        [Test]
        public async Task AddUser()
        {
            var _user = await _userService.AddUserAsync(_model);
            await _identityDbContext.Entry(_user).ReloadAsync();
            
            Assert.AreEqual(_model.FirstName, _user.FirstName);
            Assert.AreEqual(_model.LastName, _user.LastName);
            Assert.AreEqual(_model.BirthDate, _user.BirthDate);
            Assert.AreEqual(_model.Sex, _user.Sex);
            Assert.AreEqual(_model.UserName, _user.UserName);

            var author = await _authorService.GetByIdAsync(_user.AuthorId);
            var actor = await _actorService.GetByIdAsync(_user.ActorId);
            var publisher = await _publisherService.GetByIdAsync(_user.PublisherId);
            Assert.NotNull(author);
        }

        [Test]
        public async Task TaskGetUserById()
        {
            var user = await _userService.AddUserAsync(_model);

            user = await _userService.FindByIdAsync(user.Id);
        }
    }
}