using System;
using System.Collections;
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
    public class StudentSpecialityService
    {
        private readonly DbContext _dbContext;
        private readonly EventService _eventService;
        


        public StudentSpecialityService(DbContext dbContext, EventService eventService)
        {
            _dbContext = dbContext;
            _eventService = eventService;
        }

        public async Task<ActionResultModel<StudentSpeciality>> AddSpecialityAsync(Student student,
            ClassroomSpeciality classroomSpeciality, User user)
        {
            Asserts.NotNull(student, nameof(student));
            Asserts.NotNull(student.Space, nameof(student.Space));
            Asserts.NotNull(classroomSpeciality, nameof(classroomSpeciality));
            Asserts.NotNull(classroomSpeciality.Classroom, nameof(classroomSpeciality.Classroom));
            Asserts.NotNull(classroomSpeciality.Speciality, nameof(classroomSpeciality.Speciality));
            Asserts.NotNull(user, nameof(user));

            if (student.ClassroomId == null)
            {
                throw new IllegalStateException("StudentHasNoClassroom");
            }
            
            var studentSpeciality = await _AddSpecialityAsync(student, classroomSpeciality);
            await _dbContext.AddAsync(studentSpeciality);
            await _dbContext.SaveChangesAsync();

            var publisherIds = new List<string>
            {
                student.PublisherId,
                student.Space.PublisherId,
                classroomSpeciality.PublisherId,
                classroomSpeciality.Classroom!.PublisherId,
                classroomSpeciality.Speciality!.PublisherId
            };

            var @event = await _eventService.EmitAsync(publisherIds, user.ActorId, "STUDENT_SPECIALITY_ADD", classroomSpeciality);
                
            return new ActionResultModel<StudentSpeciality>(studentSpeciality, @event);
        }

        public async Task<ICollection<StudentSpeciality>> AddSpecialitiesAsync(Student student,
            HashSet<ulong> classroomSpecialityIds)
        {
            var classroomSpecialities = _dbContext.Set<ClassroomSpeciality>()
                .Where(e => classroomSpecialityIds.Contains(e.Id))
                .ToList();

            var studentSpecialities = new List<StudentSpeciality>();
            foreach (var classroomSpeciality in classroomSpecialities)
            {
                var studentSpeciality = await _AddSpecialityAsync(student, classroomSpeciality);
                await _dbContext.AddAsync(studentSpeciality);
                studentSpecialities.Add(studentSpeciality);
            }

            return studentSpecialities;
        }


        private async Task<StudentSpeciality> _AddSpecialityAsync(
            Student student,
            ClassroomSpeciality classroomSpeciality)
        {
            Asserts.NotNull(student, nameof(student));
            Asserts.NotNull(classroomSpeciality, nameof(classroomSpeciality));
            Asserts.NotNull(classroomSpeciality.Classroom, nameof(classroomSpeciality.Classroom));

            if (classroomSpeciality.ClassroomId != student.ClassroomId)
            {
                throw new InvalidOperationException("Incompatible entities.");
            }

            if (await SpecialityContainsAsync(classroomSpeciality, student))
            {
                StudentHelper.ThrowDuplicateStudentSpeciality(classroomSpeciality, student);
            }

            StudentSpeciality studentSpeciality = new()
            {
                Student = student,
                ClassroomSpeciality = classroomSpeciality
            };
            return studentSpeciality;
        }
        
        public async Task<bool> ContainsAsync(ClassroomSpeciality classroomSpeciality, string rId)
        {
            Asserts.NotNull(classroomSpeciality, nameof(classroomSpeciality));
            Asserts.NotNullOrWhiteSpace(rId, nameof(rId));

            string normalized = StringHelper.Normalize(rId);
            return await _dbContext.Set<StudentSpeciality>()
                .AnyAsync(p => classroomSpeciality.Equals(p.ClassroomSpeciality) 
                               && p.Student!.RId == normalized && p.Student.DeletedAt == null);
        }
        
        public async Task<bool> SpecialityContainsAsync(ClassroomSpeciality classroomSpeciality, Student student)
        {
            Asserts.NotNull(classroomSpeciality, nameof(classroomSpeciality));
            Asserts.NotNull(student, nameof(student));
            
            return await _dbContext.Set<StudentSpeciality>()
                .AnyAsync(p => classroomSpeciality.Equals(p.ClassroomSpeciality) 
                               && student.Equals(p.StudentId));
        }
        
        public async Task<Event> DeleteSpeciality(StudentSpeciality studentSpeciality, User user)
        {
            Asserts.NotNull(studentSpeciality, nameof(studentSpeciality));
            _dbContext.Remove(studentSpeciality);
            await _dbContext.SaveChangesAsync();
            
            var publisherIds = new List<string>
            {
                studentSpeciality.Student!.PublisherId,
                studentSpeciality.Student.Space.PublisherId,
                studentSpeciality.ClassroomSpeciality!.PublisherId,
                studentSpeciality.ClassroomSpeciality.Classroom!.PublisherId,
                studentSpeciality.ClassroomSpeciality.Speciality!.PublisherId
            };

            return await _eventService.EmitAsync(publisherIds, user.ActorId, "STUDENT_SPECIALITY_DELETE", studentSpeciality);
        }
    }
}