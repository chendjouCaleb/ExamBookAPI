using System;
using System.Threading.Tasks;
using ExamBook.Entities;
using ExamBook.Exceptions;
using ExamBook.Identity.Entities;
using ExamBook.Identity.Services;
using ExamBook.Models;
using ExamBook.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Traceability.Asserts;
using Traceability.Models;
using Traceability.Services;

#pragma warning disable NUnit2005
namespace ExamBookTest.Services
{
    public class MemberServiceTest
    {
        private IServiceProvider _provider = null!;
        private MemberService _memberService = null!;
        private SpaceService _spaceService = null!;
        private PublisherService _publisherService = null!;
        private EventAssertionsBuilder _eventAssertionsBuilder = null!;
        
        private DbContext _dbContext = null!;
        private User _user = null!;
        private User _adminUser = null!;
        private Actor _actor = null!;
        private string _userId = null!;

        private Space _space = null!;
        private MemberAddModel _model = null!;
            
        
        [SetUp]
        public async Task Setup()
        {
            var services = new ServiceCollection();
            
            _provider = services.Setup();
            _memberService = _provider.GetRequiredService<MemberService>();
            _publisherService = _provider.GetRequiredService<PublisherService>();
            _eventAssertionsBuilder = _provider.GetRequiredService<EventAssertionsBuilder>();
            _dbContext = _provider.GetRequiredService<DbContext>();

            var userService = _provider.GetRequiredService<UserService>();
            _spaceService = _provider.GetRequiredService<SpaceService>();
            _user = await userService.AddUserAsync(ServiceExtensions.UserAddModel);
            _userId = _user.Id;
            _actor = await userService.GetActor(_user);

            _adminUser = await userService.AddUserAsync(ServiceExtensions.UserAddModel2);

            var result = await _spaceService.AddAsync(_adminUser.Id, new SpaceAddModel {
                Name = "UY-1, PHILOSOPHY, L1",
                Identifier = "uy1_phi_l1"
            });
            _space = result.Item;

            _model = new MemberAddModel
            {
                IsAdmin = false,
                IsTeacher = false,
                UserId = _user.Id
            };
        }

        [Test]
        public async Task GetMemberById()
        {
            var member = (await _memberService.AddMemberAsync(_space, _model, _user)).Item;

            var result = await _memberService.GetByIdAsync(member.Id);
            
            Assert.AreEqual(member.Id, result.Id);
            Assert.NotNull(result.User);
            Assert.AreEqual(_user.Id, member.User!.Id);
        }

        [Test]
        public async Task GetMemberId_ShouldBeNull()
        {
            var id = await _memberService.IsSpaceMemberId(_space, Guid.NewGuid().ToString());
            Console.WriteLine("ID: " + id);
            Assert.Null(id);
        }


        [Test]
        public void GetNotFoundMember_ShouldThrow()
        {
            var ex = Assert.ThrowsAsync<ElementNotFoundException>(async () =>
            {
                await _memberService.GetByIdAsync(ulong.MaxValue);
            });
            Assert.AreEqual("MemberNotFoundById", ex!.Code);
            Assert.AreEqual(ulong.MaxValue, ex.Params[0]);
        }

        [Test]
        public async Task AddMember()
        {
            var result = await _memberService.AddMemberAsync(_space, _model, _user);
            var member = result.Item;
            
            await _dbContext.Entry(member).ReloadAsync();
            
            Assert.AreEqual(_model.UserId, member.UserId);
            Assert.AreEqual(_model.IsAdmin, member.IsAdmin);
            Assert.AreEqual(_space.Id, member.SpaceId);

            var publisher = await _publisherService.GetByIdAsync(member.PublisherId);
            var spacePublisher = await _publisherService.GetByIdAsync(_space.PublisherId);
            var addEvent = result.Event;

            Assert.NotNull(publisher);
            _eventAssertionsBuilder.Build(addEvent)
                .HasName("MEMBER_ADD")
                .HasActor(_actor)
                .HasPublisher(publisher)
                .HasPublisher(spacePublisher)
                .HasData(member);
        }

        
        [Test]
        public async Task TryAddUseTwiceInSpace_ShouldThrow()
        {
            await _memberService.AddMemberAsync(_space, _model, _user);

            var ex = Assert.ThrowsAsync<IllegalOperationException>(async () =>
            {
                await _memberService.AddMemberAsync(_space, _model, _user);
            });
            
            Assert.AreEqual("UserIsAlreadyMember", ex!.Message);
        }


        [Test]
        public async Task GetOrAdd_Add()
        {
            Assert.False(await _memberService.IsSpaceMember(_space, _user.Id));
            var member = await _memberService.GetOrAddAsync(_space, _user.Id, _user);
            Assert.NotNull(member);
            Assert.AreEqual(member.UserId, _user.Id);
            Assert.False(member.IsAdmin);
            Assert.False(member.IsTeacher);
        }
        
        
        [Test]
        public async Task GetOrAdd_Get()
        {
            var member = (await _memberService.AddMemberAsync(_space, _model, _user)).Item;
            var getMember = await _memberService.GetOrAddAsync(_space, _user.Id, _user);
            Assert.AreEqual(member.Id, getMember.Id);
        }

       
        
       
        [Test]
        public async Task ToggleAdminToTrue()
        {
            var member = (await _memberService.AddMemberAsync(_space, _model, _user)).Item;
            await _dbContext.Entry(member).ReloadAsync();

            var result = await _memberService.ToggleAdminAsync(member, _user);
            await _dbContext.Entry(member).ReloadAsync();

            var publisher = await _publisherService.GetByIdAsync(member.PublisherId);
            var spacePublisher = await _publisherService.GetByIdAsync(_space.PublisherId);
            var userPublisher = await _publisherService.GetByIdAsync(_user.PublisherId); 
          
            var @event = result.Event;

            Assert.True(member.IsAdmin);
         
            _eventAssertionsBuilder.Build(@event)
                .HasName("MEMBER_SET_ADMIN")
                .HasActor(_actor)
                .HasPublisher(publisher)
                .HasPublisher(spacePublisher)
                .HasPublisher(userPublisher)
                .HasData(new {});
        }
        
        
        [Test]
        public async Task ToggleAdminToFalse()
        {
            var member = (await _memberService.AddMemberAsync(_space, _model, _user)).Item;
            await _memberService.ToggleAdminAsync(member, _user);

            var result = await _memberService.ToggleAdminAsync(member, _user);
            await _dbContext.Entry(member).ReloadAsync();

            var publisher = await _publisherService.GetByIdAsync(member.PublisherId);
            var spacePublisher = await _publisherService.GetByIdAsync(_space.PublisherId);
            var userPublisher = await _publisherService.GetByIdAsync(_user.PublisherId); 
          
            var @event = result.Event;

            Assert.False(member.IsAdmin);
         
            _eventAssertionsBuilder.Build(@event)
                .HasName("MEMBER_UNSET_ADMIN")
                .HasActor(_actor)
                .HasPublisher(publisher)
                .HasPublisher(spacePublisher)
                .HasPublisher(userPublisher)
                .HasData(new {});
        }
        
        
        
        [Test]
        public async Task ToggleAdminToFalse_WithNoAdmin_ShouldThrow()
        {
            var member = await _memberService.GetAsync(_space, _adminUser.Id);
            Assert.True(member!.IsAdmin);

            var ex = Assert.ThrowsAsync<IllegalOperationException>(async () =>
            {
                await _memberService.ToggleAdminAsync(member, _user);
            });
            Assert.AreEqual("SpaceOnlyOneAdmin", ex!.Code);
        }
        
        
        
        [Test]
        public async Task ToggleTeacherToTrue()
        {
            var member = (await _memberService.AddMemberAsync(_space, _model, _user)).Item;
            await _dbContext.Entry(member).ReloadAsync();

            var result = await _memberService.ToggleTeacherAsync(member, _user);
            await _dbContext.Entry(member).ReloadAsync();

            var publisher = await _publisherService.GetByIdAsync(member.PublisherId);
            var spacePublisher = await _publisherService.GetByIdAsync(_space.PublisherId);
            var userPublisher = await _publisherService.GetByIdAsync(_user.PublisherId); 
          
            var @event = result.Event;

            Assert.True(member.IsTeacher);
         
            _eventAssertionsBuilder.Build(@event)
                .HasName("MEMBER_SET_TEACHER")
                .HasActor(_actor)
                .HasPublisher(publisher)
                .HasPublisher(spacePublisher)
                .HasPublisher(userPublisher)
                .HasData(new {});
        }
        
        
        [Test]
        public async Task ToggleTeacherToFalse()
        {
            var member = (await _memberService.AddMemberAsync(_space, _model, _user)).Item;
            await _memberService.ToggleTeacherAsync(member, _user);

            var result = await _memberService.ToggleTeacherAsync(member, _user);
            await _dbContext.Entry(member).ReloadAsync();

            var publisher = await _publisherService.GetByIdAsync(member.PublisherId);
            var spacePublisher = await _publisherService.GetByIdAsync(_space.PublisherId);
            var userPublisher = await _publisherService.GetByIdAsync(_user.PublisherId); 
          
            var @event = result.Event;

            Assert.False(member.IsTeacher);
         
            _eventAssertionsBuilder.Build(@event)
                .HasName("MEMBER_UNSET_TEACHER")
                .HasActor(_actor)
                .HasPublisher(publisher)
                .HasPublisher(spacePublisher)
                .HasPublisher(userPublisher)
                .HasData(new {});
        }

        [Test]
        public async Task DeleteMember()
        {
            var member = (await _memberService.AddMemberAsync(_space, _model, _user)).Item;
            
            var deleteEvent = await _memberService.DeleteAsync(member, _user);
            await _dbContext.Entry(member).ReloadAsync();
            
            Assert.False(member.IsAdmin);
            Assert.NotNull(member.DeletedAt);
            Assert.True(member.IsDeleted);

            var publisher = await _publisherService.GetByIdAsync(member.PublisherId);
            var spacePublisher = await _publisherService.GetByIdAsync(_space.PublisherId);

            _eventAssertionsBuilder.Build(deleteEvent)
                .HasName("MEMBER_DELETE")
                .HasActor(_actor)
                .HasPublisher(publisher)
                .HasPublisher(spacePublisher)
                .HasData(new {});
        }


        [Test]
        public async Task IsSpaceMember_WithMember_ShouldBeTrue()
        {
            await _memberService.AddMemberAsync(_space, _model, _user);
            Assert.True(await _memberService.IsSpaceMember(_space, _user.Id));
        }


        [Test]
        public async Task IsSpaceMember_WithNonMember_ShouldBeFalse()
        {
            var isMember = await _memberService.IsSpaceMember(_space, Guid.NewGuid().ToString());
            Assert.False(isMember);
        }

        [Test]
        public async Task IsMember_WithDeletedUser_ShouldBeFalse()
        {
            var member = (await _memberService.AddMemberAsync(_space, _model, _adminUser)).Item;
            await _memberService.DeleteAsync(member, _adminUser);
            var isMember = await _memberService.IsSpaceMember(_space, _user.Id);
            Assert.False(isMember);
        }
      
    }
}