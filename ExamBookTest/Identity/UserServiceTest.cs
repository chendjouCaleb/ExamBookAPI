using System;
using System.Threading.Tasks;
using ExamBook.Identity;
using ExamBook.Models;
using ExamBook.Persistence;
using ExamBook.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Social.Services;

namespace ExamBookTest.Identity
{
    public class UserServiceTest
    {
        private IServiceProvider _provider = null!;
        private UserService _userService = null!;
        private AuthorService _authorService = null!;
        private DbContext _dbContext = null!;
        private ApplicationIdentityDbContext _identityDbContext = null!;
        private UserAddModel _model = new ()
        {
            FirstName = "first name",
            LastName = "last name",
            Sex = 'M',
            BirthDate = new DateTime(1995,1,1),
            UserName = "userName",
            Password = "Password"
        };
            
        
        [SetUp]
        public void Setup()
        {
            var services = new ServiceCollection();
            services.Setup();
            
            _provider = services.BuildServiceProvider();
            _userService = _provider.GetRequiredService<UserService>();
            _dbContext = _provider.GetRequiredService<DbContext>();
            _identityDbContext = _provider.GetRequiredService<ApplicationIdentityDbContext>();
            _authorService = _provider.GetRequiredService<AuthorService>();
            _dbContext.Database.EnsureDeleted();
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
            Assert.NotNull(author);
        }
    }
}