using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ExamBook.Entities;
using ExamBook.Exceptions;
using ExamBook.Helpers;
using ExamBook.Identity.Models;
using ExamBook.Models;
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
        private readonly PublisherService _publisherService;

        public StudentService(DbContext dbContext, EventService eventService, PublisherService publisherService)
        {
            _dbContext = dbContext;
            _eventService = eventService;
            _publisherService = publisherService;
        }


        public async Task<ActionResultModel<Student>> AddAsync(Classroom classroom, StudentAddModel model, User user)
        {
            Asserts.NotNull(classroom, nameof(classroom));
            Asserts.NotNull(classroom.Space, nameof(classroom.Space));
            Asserts.NotNull(model, nameof(model));
            Asserts.NotNull(user, nameof(user));

            var space = classroom.Space!;
            if (await ContainsAsync(space, model.RId))
            {
                StudentHelper.ThrowDuplicateRId(classroom, model.RId);
            }
            string normalizedRid = model.RId.Normalize().ToUpper();
            var publisher = await _publisherService.AddAsync();
            
            Student student = new()
            {
                FirstName = model.FirstName,
                LastName = model.LastName,
                BirthDate = model.BirthDate,
                Sex = model.Sex,
                NormalizedRId = normalizedRid,
                RId = model.RId,
                Classroom = classroom,
                Space = space,
                PublisherId = publisher.Id
            };
            await _dbContext.AddAsync(student);
            await _dbContext.SaveChangesAsync();

            var publisherIds = new List<string> {publisher.Id, space.PublisherId, classroom.PublisherId};
            var @event = await _eventService.EmitAsync(publisherIds, user.ActorId, "STUDENT_ADD", student);
            
            return new ActionResultModel<Student>(student, @event);
        }

        

        public async Task ChangeRId(Student student, StudentChangeRIdModel model)
        {
            Asserts.NotNull(student, nameof(student));
            Asserts.NotNull(student.Space, nameof(student.Space));
            Asserts.NotNull(model, nameof(model));
            if (await ContainsAsync(student.Space, model.RId))
            {
                StudentHelper.ThrowDuplicateRId(student.Classroom, model.RId);
            }
            string normalizedRid = model.RId.Normalize().ToUpper();
            student.RId = model.RId;
            student.NormalizedRId = normalizedRid;
            _dbContext.Update(student);
            await _dbContext.SaveChangesAsync();
        }


        public async Task ChangeInfo(Student student, StudentChangeInfoModel model)
        {
            Asserts.NotNull(student, nameof(student));
            Asserts.NotNull(student.Classroom, nameof(student.Classroom));
            Asserts.NotNull(model, nameof(model));

            student.Sex = model.Sex;
            student.BirthDate = model.BirthDate;
            student.FirstName = model.FirstName;
            student.LastName = model.LastName;
            _dbContext.Update(student);
            await _dbContext.SaveChangesAsync();
        }
        
        


        public async Task<bool> ContainsAsync(Space space, string rId)
        {
            Asserts.NotNull(space, nameof(space));
            Asserts.NotNullOrWhiteSpace(rId, nameof(rId));

            string normalized = rId.Normalize().ToUpper();
            return await _dbContext.Set<Student>()
                .AnyAsync(s => space.Id == s.SpaceId && s.RId == normalized);
        }
        
        
        
        
        

        public async Task<Student?> FindAsync(Space space, string rId)
        {
            Asserts.NotNull(space, nameof(space));
            Asserts.NotNullOrWhiteSpace(rId, nameof(rId));

            string normalized = rId.Normalize().ToUpper();
            var student = await _dbContext.Set<Student>()
                .FirstOrDefaultAsync(s => space.Id == s.SpaceId && s.RId == normalized);

            if (student == null)
            {
                throw new ElementNotFoundException("StudentNotFoundByRId");
            }

            return student;
        }

        
        


        public async Task MarkAsDeleted(Student student)
        {
            Asserts.NotNull(student, nameof(student));
            student.Sex = '0';
            student.BirthDate = DateTime.MinValue;
            student.FirstName = "";
            student.LastName = "";
            student.RId = "";
            student.NormalizedRId = "";
            student.DeletedAt = DateTime.Now;
            _dbContext.Update(student);
            await _dbContext.SaveChangesAsync();
        }

        public async Task<Event> DeleteAsync(Student student, User user)
        {
            Asserts.NotNull(student, nameof(student));
            Asserts.NotNull(student.Space, nameof(student.Space));
            Asserts.NotNull(student.Classroom, nameof(student.Classroom));
            Asserts.NotNull(user, nameof(user));
           // var studentSpecialities = await _dbContext.Set<StudentSpeciality>()
           //      .Where(p => student.Equals(p.StudentId))
           //      .ToListAsync();

           student.FirstName = "";
           student.LastName = "";
           student.Sex = '0';
           student.BirthDate = DateTime.MinValue;
           student.RId = "";

           _dbContext.Update(student);
           await _dbContext.SaveChangesAsync();
           
           var publisherIds = new List<string> {
               student.PublisherId, 
               student.Space.PublisherId
           };

           if (student.ClassroomId != 0)
           {
               publisherIds.Add(student.Classroom.PublisherId);
           }
           
           return await _eventService.EmitAsync(publisherIds, user.ActorId, "STUDENT_DELETE", student);
        }
    }
}