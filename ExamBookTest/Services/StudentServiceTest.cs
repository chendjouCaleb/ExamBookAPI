using System;
using System.Threading.Tasks;
using ExamBook.Entities;
using ExamBook.Exceptions;
using ExamBook.Identity.Entities;
using ExamBook.Identity.Services;
using ExamBook.Models;
using ExamBook.Models.Data;
using ExamBook.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Social.Helpers;
using Vx.Asserts;
using Vx.Models;
using Vx.Services;

#pragma warning disable NUnit2005
namespace ExamBookTest.Services
{
    public class StudentServiceTest
    {
        private IServiceProvider _provider = null!;
        private StudentService _service = null!;
        private MemberService _memberService = null!;
        private SpaceService _spaceService = null!;
        private PublisherService _publisherService = null!;
        private EventAssertionsBuilder _eventAssertionsBuilder = null!;
        
        private DbContext _dbContext = null!;
        private User _adminUser = null!;
        private User _user = null!;
        private Actor _actor = null!;

        private Space _space = null!;
        private StudentAddModel _model = null!;
            
        
        [SetUp]
        public async Task Setup()
        {
            var services = new ServiceCollection();
            
            _provider = services.Setup();
            _publisherService = _provider.GetRequiredService<PublisherService>();
            _eventAssertionsBuilder = _provider.GetRequiredService<EventAssertionsBuilder>();
            _dbContext = _provider.GetRequiredService<DbContext>();

            var userService = _provider.GetRequiredService<UserService>();
            _spaceService = _provider.GetRequiredService<SpaceService>();
            _memberService = _provider.GetRequiredService<MemberService>();
            _service = _provider.GetRequiredService<StudentService>();
            _adminUser = await userService.AddUserAsync(ServiceExtensions.UserAddModel);
            _user = await userService.AddUserAsync(ServiceExtensions.UserAddModel2);
            _actor = await userService.GetActor(_adminUser);

            var result = await _spaceService.AddAsync(_adminUser.Id, new SpaceAddModel {
                Name = "UY-1, PHILOSOPHY, L1",
                Identifier = "uy1_phi_l1"
            });
            _space = result.Item;

            _model = new StudentAddModel
            {
                FirstName = "first name",
                LastName = "last name",
                Code = "8say6g3",
                BirthDate = new DateTime(1990, 1, 1),
                Sex = 'm'
            };
        }


        [Test]
        public async Task GetStudent()
        {
            var result = await _service.AddAsync(_space, _model, _adminUser);
            var student = await _service.GetByIdAsync(result.Item.Id);
            
            Assert.AreEqual(result.Item.Id, student.Id);
            Assert.NotNull(student.Space);
        }

        [Test]
        public async Task GetStudentWithUser()
        {
            var model = new StudentAddModel
            {
                FirstName = "first name",
                LastName = "last name",
                Code = "8say6g3",
                BirthDate = new DateTime(1990, 1, 1),
                Sex = 'm',
                UserId = _user.Id
            };
            var result = await _service.AddAsync(_space, model, _adminUser);
            var student = await _service.GetByIdAsync(result.Item.Id);
            Assert.NotNull(student.Member);
            Assert.NotNull(student.Member!.User);
        }

        [Test]
        public void GetNotFoundStudent_ShouldThrow()
        {
            var ex = Assert.ThrowsAsync<ElementNotFoundException>(async () =>
            {
                await _service.GetByIdAsync(ulong.MaxValue);
            });
            
            Assert.AreEqual("StudentNotFoundById", ex!.Code);
            Assert.AreEqual(ulong.MaxValue, ex.Params[0]);
        }


        [Test]
        public async Task AddStudentAsync()
        {
            var result = await _service.AddAsync(_space, _model, _adminUser);
            var student = result.Item;
            await _dbContext.Entry(student).ReloadAsync();
            
            Assert.AreEqual(_space.Id, student.SpaceId);
            Assert.AreEqual(_model.FirstName, student.FirstName);
            Assert.AreEqual(_model.LastName, student.LastName);
            Assert.AreEqual(_model.Sex, student.Sex);
            Assert.AreEqual(_model.Code, student.Code);
            Assert.AreEqual(StringHelper.Normalize(_model.Code), student.NormalizedCode);
            Assert.IsNotEmpty(student.PublisherId);

            var publisher = await _publisherService.GetByIdAsync(student.PublisherId);
            var spacePublisher = await _publisherService.GetByIdAsync(_space.PublisherId);
            
            _eventAssertionsBuilder.Build(result.Event)
                .HasName("STUDENT_ADD")
                .HasActor(_actor)
                .HasPublisher(publisher)
                .HasPublisher(spacePublisher)
                .HasData(student);
        }

        

        [Test]
        public async Task AddStudentWithUser()
        {
            var model = new StudentAddModel
            {
                FirstName = "first name",
                LastName = "last name",
                Code = "8say6g3",
                BirthDate = new DateTime(1990, 1, 1),
                Sex = 'm',
                UserId = _user.Id
            };
            
            var result = await _service.AddAsync(_space, model, _adminUser);
            var student = result.Item;
            await _dbContext.Entry(student).ReloadAsync();
            var member = await _dbContext.Set<Member>().FindAsync(student.MemberId);
            
            Assert.NotNull(member);
            Assert.AreEqual(student.SpaceId, member!.SpaceId);

            var userPublisher = await _publisherService.GetByIdAsync(_user.PublisherId);
            var memberPublisher = await _publisherService.GetByIdAsync(member.PublisherId);
            var publisher = await _publisherService.GetByIdAsync(student.PublisherId);
            var spacePublisher = await _publisherService.GetByIdAsync(_space.PublisherId);
            
            _eventAssertionsBuilder.Build(result.Event)
                .HasName("STUDENT_ADD")
                .HasActor(_actor)
                .HasPublisher(publisher)
                .HasPublisher(spacePublisher)
                .HasPublisher(userPublisher)
                .HasPublisher(memberPublisher)
                .HasData(student);
        }
        
        [Test]
        public async Task AddStudent_WithUsedCode_ShouldThrow()
        {
            await _service.AddAsync(_space, _model, _adminUser);
            var ex = Assert.ThrowsAsync<UsedValueException>(async () =>
            {
                await _service.AddAsync(_space, _model, _adminUser);
            });
            
            Assert.AreEqual("StudentCodeUsed", ex!.Message);
            Assert.AreEqual(_model.Code, ex.Params[0]);
        }

        [Test]
        public async Task AttachStudentToUser()
        {
            
            var student = (await _service.AddAsync(_space, _model, _adminUser)).Item;
            await _dbContext.Entry(student).ReloadAsync();

            var member = await _memberService.GetOrAddAsync(_space, _user.Id, _adminUser);
            var result = await _service.AttachAsync(student, member, _adminUser);
            
            Assert.NotNull(member);
            Assert.AreEqual(student.SpaceId, member.SpaceId);

            var userPublisher = await _publisherService.GetByIdAsync(_user.PublisherId);
            var memberPublisher = await _publisherService.GetByIdAsync(member.PublisherId);
            var publisher = await _publisherService.GetByIdAsync(student.PublisherId);
            var spacePublisher = await _publisherService.GetByIdAsync(_space.PublisherId);
            
            _eventAssertionsBuilder.Build(result)
                .HasName("STUDENT_ATTACH_MEMBER")
                .HasActor(_actor)
                .HasPublisher(publisher)
                .HasPublisher(spacePublisher)
                .HasPublisher(userPublisher)
                .HasPublisher(memberPublisher)
                .HasData(new {MemberId = member.Id});
        }
        
        
        [Test]
        public async Task AttachAttachedStudent_ShouldThrow()
        {
            var student = (await _service.AddAsync(_space, _model, _adminUser)).Item;
            var member1 = await _memberService.GetOrAddAsync(_space, _user.Id, _adminUser);
            var member2 = await _memberService.GetOrAddAsync(_space, _adminUser.Id, _adminUser);
            await _service.AttachAsync(student, member1, _adminUser);

            var ex = Assert.ThrowsAsync<IllegalOperationException>(async () =>
            {
                await _service.AttachAsync(student, member2, _adminUser);
            });
            Assert.AreEqual("StudentHasMember", ex!.Code);
        }
        
        public async Task AttachStudentToAttachedMember_ShouldThrow()
        {
            var student = (await _service.AddAsync(_space, _model, _adminUser)).Item;
            var member = await _memberService.GetOrAddAsync(_space, _user.Id, _adminUser);
            await _service.AttachAsync(student, member, _adminUser);

            var ex = Assert.ThrowsAsync<IllegalOperationException>(async () =>
            {
                await _service.AttachAsync(student, member, _adminUser);
            });
            Assert.AreEqual("MemberHasStudent", ex!.Code);
        }
        
        
        [Test]
        public async Task DeleteStudentAsync()
        {
            var result = await _service.AddAsync(_space, _model, _adminUser);
            var student = result.Item;

            var deleteEvent = await _service.DeleteAsync(student, _adminUser);
            await _dbContext.Entry(student).ReloadAsync();
            
            Assert.NotNull(student.DeletedAt);
            Assert.AreEqual("", student.FirstName);
            Assert.AreEqual("", student.LastName);
            Assert.AreEqual('0', student.Sex);
            Assert.AreEqual("", student.Code);
            Assert.AreEqual("", student.NormalizedCode);

            var publisher = await _publisherService.GetByIdAsync(student.PublisherId);
            var spacePublisher = await _publisherService.GetByIdAsync(_space.PublisherId);
            
            _eventAssertionsBuilder.Build(deleteEvent)
                .HasName("STUDENT_DELETE")
                .HasActor(_actor)
                .HasPublisher(publisher)
                .HasPublisher(spacePublisher)
                .HasData(student);
        }
        
        [Test]
        public async Task ChangeStudentCode()
        {
            Student student = (await _service.AddAsync (_space, _model, _adminUser)).Item;
            var newCode = "9632854";

            var eventData = new ChangeValueData<string>(student.Code, newCode);
            var changeEvent = await _service.ChangeCodeAsync(student, newCode, _adminUser);

            await _dbContext.Entry(student).ReloadAsync();

            Assert.AreEqual(newCode, student.Code);
            Assert.AreEqual(StringHelper.Normalize(newCode), student.NormalizedCode);

            var publisher = await _publisherService.GetByIdAsync(student.PublisherId);
            var spacePublisher = await _publisherService.GetByIdAsync(_space.PublisherId);

            Assert.NotNull(publisher);
            _eventAssertionsBuilder.Build(changeEvent)
                .HasName("STUDENT_CHANGE_CODE")
                .HasActor(_actor)
                .HasPublisher(publisher)
                .HasPublisher(spacePublisher)
                .HasData(eventData);
        }
        
        
        [Test]
        public async Task TryChangeStudentCode_WithUsedCode_ShouldThrow()
        {
            var student = (await _service.AddAsync(_space, _model, _adminUser)).Item;

            var ex = Assert.ThrowsAsync<UsedValueException>(async () =>
            {
                await _service.ChangeCodeAsync(student, student.Code, _adminUser);
            });
            Assert.AreEqual("StudentCodeUsed", ex!.Message);
        }
        
        
        [Test]
        public async Task IsStudent()
        {
            var student = (await _service.AddAsync(_space, _model, _adminUser)).Item;
            var hasStudent = await _service.ContainsAsync(_space, student.Code);
            Assert.True(hasStudent);
        }


        [Test]
        public async Task IsStudent_WithNonStudent_ShouldBeFalse()
        {
            var hasStudent = await _service.ContainsAsync(_space, "5D");
            Assert.False(hasStudent);
        }

        [Test]
        public async Task IsStudent_WithDeletedStudent_ShouldBeFalse()
        {
            var student = (await _service.AddAsync(_space, _model, _adminUser)).Item;
            var rId = student.Code;
            
            await _service.DeleteAsync(student, _adminUser);
            
            var hasStudent = await _service.ContainsAsync(_space, rId);
            Assert.False(hasStudent);
        }

    }
}