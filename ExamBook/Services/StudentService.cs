using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using ExamBook.Entities;
using ExamBook.Exceptions;
using ExamBook.Helpers;
using ExamBook.Identity.Entities;
using ExamBook.Identity.Services;
using ExamBook.Models;
using ExamBook.Models.Data;
using ExamBook.Utils;
using Microsoft.EntityFrameworkCore;
using Vx.Models;
using Vx.Services;

namespace ExamBook.Services
{
    public class StudentService
    {
        private readonly DbContext _dbContext;
        private readonly EventService _eventService;
        private readonly UserService _userService;
        private readonly MemberService _memberService;
        private readonly PublisherService _publisherService;
        private readonly StudentSpecialityService _studentSpecialityService;

        public StudentService(DbContext dbContext, 
            EventService eventService, 
            PublisherService publisherService, 
            StudentSpecialityService studentSpecialityService, UserService userService, MemberService memberService)
        {
            _dbContext = dbContext;
            _eventService = eventService;
            _publisherService = publisherService;
            _studentSpecialityService = studentSpecialityService;
            _userService = userService;
            _memberService = memberService;
        }

        public async Task<Student> GetByIdAsync(ulong studentId)
        {
            var student = await _dbContext.Set<Student>()
                .Include(s => s.Space)
                .Include(m => m.Member)
                .Where(s => s.Id == studentId)
                .FirstOrDefaultAsync();

            if (student == null)
            {
                throw new ElementNotFoundException("StudentNotFoundById", studentId);
            }

            if (student.Member != null)
            {
                student.Member.User = await _userService.GetByIdAsync(student.Member.UserId!);
            }

            return student;
        }


        public async Task<ActionResultModel<Student>> AddAsync(Space space, StudentAddModel model, User user)
        {
            AssertHelper.NotNull(space, nameof(space));
            AssertHelper.NotNull(model, nameof(model));
            AssertHelper.NotNull(user, nameof(user));

            
            if (await ContainsAsync(space, model.Code))
            {
                throw new UsedValueException("StudentCodeUsed", model.Code);
            }
            
            string normalizedCode = model.Code.Normalize().ToUpper();
            var specialities = _dbContext.Set<Speciality>()
                .Where(e => model.SpecialityIds.Contains(e.Id))
                .ToList();

            var publisherIds = new List<string> {space.PublisherId}.ToImmutableList();
            
            var publisher = await _publisherService.AddAsync();
            Student student = new()
            {
                FirstName = model.FirstName,
                LastName = model.LastName,
                BirthDate = model.BirthDate,
                Sex = model.Sex,
                NormalizedCode = normalizedCode,
                Code = model.Code,
                Space = space,
                SpaceId = space.Id,
                PublisherId = publisher.Id
            };
            publisherIds = publisherIds.Add(publisher.Id);
            
            if (!string.IsNullOrEmpty(model.UserId))
            {
                var member = await _memberService.GetOrAddAsync(space, model.UserId, user);

                if (await ContainsAsync(space, member))
                {
                    throw new UsedValueException("MemberHasStudent");
                }
                
                publisherIds = publisherIds.Add(member.PublisherId).Add(member.User!.PublisherId);
                student.Member = member;
            }
            
            
            student.Specialities = (await _studentSpecialityService.CreateSpecialitiesAsync(student, specialities)).ToList();
            
            await _dbContext.AddAsync(student);
            await _dbContext.SaveChangesAsync();

            
            publisherIds = publisherIds.AddRange(specialities.Select(s => s.PublisherId));
            
            var @event = await _eventService.EmitAsync(publisherIds, user.ActorId, "STUDENT_ADD", student);
            
            return new ActionResultModel<Student>(student, @event);
        }

        
        
        public async Task<Event> AttachAsync(Student student, Member member, User user)
        {
            AssertHelper.NotNull(user, nameof(user));
            AssertHelper.NotNull(member.Space, nameof(member.Space));
            AssertHelper.NotNull(student.Space, nameof(student.Space));
            AssertHelper.IsTrue(student.SpaceId == member.SpaceId);
            
            if (await ContainsAsync(student.Space, member))
            {
                throw new UsedValueException("MemberHasStudent");
            }

            if (student.MemberId != null && student.MemberId != 0)
            {
                throw new IllegalOperationException("StudentHasMember");
            }


            student.Member = member;
            _dbContext.Update(student);
            await _dbContext.SaveChangesAsync();

            var publisherIds = new List<string> { 
                student.PublisherId,
                student.Space.PublisherId,
                member.PublisherId,
                member.User!.PublisherId
            };
            return await _eventService.EmitAsync(publisherIds, user.ActorId, 
                "STUDENT_ATTACH_MEMBER", new {MemberId = member.Id});
            
        }
        
        
        public async Task<Event> DetachAsync(Student student, User user)
        {
            AssertHelper.NotNull(user, nameof(user));
            AssertHelper.NotNull(student.Space, nameof(student.Space));
            
            if (student.MemberId == null && student.MemberId == 0)
            {
                throw new IllegalOperationException("StudentHasNotMember");
            }

            var member = student.Member!;

            student.Member = null;
            student.MemberId = null;
            _dbContext.Update(student);
            await _dbContext.SaveChangesAsync();

            var publisherIds = new List<string> { 
                student.PublisherId,
                student.Space.PublisherId,
                member.PublisherId,
                member.User!.PublisherId
            };
            return await _eventService.EmitAsync(publisherIds, user.ActorId, "STUDENT_DETACH_MEMBER", new {member.Id});
            
        }

        public async Task<Event> ChangeCodeAsync(Student student, string code, User user)
        {
            AssertHelper.NotNull(user, nameof(user));
            AssertHelper.NotNull(student, nameof(student));
            AssertHelper.NotNull(student.Space, nameof(student.Space));
            
            if (await ContainsAsync(student.Space, code))
            {
                throw new UsedValueException("StudentCodeUsed", code);
            }

            var data = new ChangeValueData<string>(student.Code, code);
            string normalizedCode = StringHelper.Normalize(code);
            student.Code = code;
            student.NormalizedCode = normalizedCode;
            _dbContext.Update(student);
            await _dbContext.SaveChangesAsync();

            var publisherIds = new List<string> { student.PublisherId, student.Space.PublisherId };
            return await _eventService.EmitAsync(publisherIds, user.ActorId, "STUDENT_CHANGE_CODE", data);
        }


        public async Task<Event> ChangeInfoAsync(Student student, StudentChangeInfoModel model, User user)
        {
            AssertHelper.NotNull(student, nameof(student));
            AssertHelper.NotNull(student.Space, nameof(student.Space));
            AssertHelper.NotNull(model, nameof(model));
            AssertHelper.NotNull(user, nameof(user));

            student.Sex = model.Sex;
            student.BirthDate = model.BirthDate;
            student.FirstName = model.FirstName;
            student.LastName = model.LastName;
            _dbContext.Update(student);
            await _dbContext.SaveChangesAsync();
            
            var publisherIds = new List<string> { student.PublisherId, student.Space.PublisherId };
      
            return await _eventService.EmitAsync(publisherIds, user.ActorId, "STUDENT_CHANGE_INFO", new {});
        }
        
        


        public async Task<bool> ContainsAsync(Space space, string code)
        {
            AssertHelper.NotNull(space, nameof(space));
            AssertHelper.NotNullOrWhiteSpace(code, nameof(code));

            string normalized = StringHelper.Normalize(code);
            return await _dbContext.Set<Student>()
                .AnyAsync(s => space.Id == s.SpaceId && s.NormalizedCode == normalized && s.DeletedAt == null);
        }
        
        public async Task<bool> ContainsAsync(Space space, Member member)
        {
            AssertHelper.NotNull(space, nameof(space));
            AssertHelper.NotNull(member, nameof(member));

            return await _dbContext.Set<Student>()
                .AnyAsync(s => space.Id == s.SpaceId && s.MemberId == member.Id && s.DeletedAt == null);
        }
        
        
        
        

        public async Task<Student?> FindByCodeAsync(Space space, string code)
        {
            AssertHelper.NotNull(space, nameof(space));
            AssertHelper.NotNullOrWhiteSpace(code, nameof(code));

            string normalized = code.Normalize().ToUpper();
            var student = await _dbContext.Set<Student>()
                .FirstOrDefaultAsync(s => space.Id == s.SpaceId && s.Code == normalized);

            if (student == null)
            {
                throw new ElementNotFoundException("StudentNotFoundByCode");
            }

            return student;
        }

        
        


        public async Task MarkAsDeleted(Student student)
        {
            AssertHelper.NotNull(student, nameof(student));
            student.Sex = '0';
            student.BirthDate = DateTime.MinValue;
            student.FirstName = "";
            student.LastName = "";
            student.Code = "";
            student.NormalizedCode = "";
            student.DeletedAt = DateTime.Now;
            _dbContext.Update(student);
            await _dbContext.SaveChangesAsync();
        }

        public async Task<Event> DeleteAsync(Student student, User user)
        {
            AssertHelper.NotNull(student, nameof(student));
            AssertHelper.NotNull(student.Space, nameof(student.Space));
            AssertHelper.NotNull(user, nameof(user));
           // var studentSpecialities = await _dbContext.Set<StudentSpeciality>()
           //      .Where(p => student.Equals(p.StudentId))
           //      .ToListAsync();

           student.FirstName = "";
           student.LastName = "";
           student.Sex = '0';
           student.BirthDate = DateTime.MinValue;
           student.Code = "";
           student.NormalizedCode = "";
           student.DeletedAt = DateTime.Now;

           _dbContext.Update(student);
           await _dbContext.SaveChangesAsync();
           
           var publisherIds = new List<string> {
               student.PublisherId, 
               student.Space.PublisherId
           };

           return await _eventService.EmitAsync(publisherIds, user.ActorId, "STUDENT_DELETE", student);
        }
    }
}