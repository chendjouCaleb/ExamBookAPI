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
            Speciality speciality, User user)
        {
            Asserts.NotNull(student, nameof(student));
            Asserts.NotNull(student.Space, nameof(student.Space));
            Asserts.NotNull(speciality, nameof(speciality));
            Asserts.NotNull(user, nameof(user));

            var studentSpeciality = await CreateSpecialityAsync(student, speciality);
            await _dbContext.AddAsync(studentSpeciality);
            await _dbContext.SaveChangesAsync();

            var publisherIds = new List<string>
            {
                student.PublisherId,
                student.Space.PublisherId,
                speciality.PublisherId
            };

            var @event = await _eventService.EmitAsync(publisherIds, user.ActorId, "STUDENT_SPECIALITY_ADD", studentSpeciality);
                
            return new ActionResultModel<StudentSpeciality>(studentSpeciality, @event);
        }

        public async Task<ICollection<StudentSpeciality>> AddSpecialitiesAsync(Student student,
            HashSet<ulong> specialityIds)
        {
            var specialities = _dbContext.Set<Speciality>()
                .Where(e => specialityIds.Contains(e.Id))
                .ToList();

            var studentSpecialities = new List<StudentSpeciality>();
            foreach (var speciality in specialities)
            {
                var studentSpeciality = await CreateSpecialityAsync(student, speciality);
                await _dbContext.AddAsync(studentSpeciality);
                studentSpecialities.Add(studentSpeciality);
            }

            return studentSpecialities;
        }

        
        public async Task<ICollection<StudentSpeciality>> CreateSpecialitiesAsync(Student student,
            ICollection<Speciality> specialities)
        {
            var studentSpecialities = new List<StudentSpeciality>();
            foreach (var speciality in specialities)
            {
                var studentSpeciality = await CreateSpecialityAsync(student, speciality);
                studentSpecialities.Add(studentSpeciality);
            }

            return studentSpecialities;
        }

        private async Task<StudentSpeciality> CreateSpecialityAsync(Student student, Speciality speciality)
        {
            Asserts.NotNull(student, nameof(student));
            Asserts.NotNull(speciality, nameof(speciality));

            if (speciality.SpaceId != student.SpaceId)
            {
                throw new InvalidOperationException("Incompatible entities.");
            }

            if (await SpecialityContainsAsync(speciality, student))
            {
                throw new IllegalOperationException("StudentSpecialityAlreadyExists");
            }

            StudentSpeciality studentSpeciality = new()
            {
                Student = student,
                Speciality = speciality
            };
            return studentSpeciality;
        }
        
        public async Task<bool> ContainsAsync(Speciality speciality, string rId)
        {
            Asserts.NotNull(speciality, nameof(speciality));
            Asserts.NotNullOrWhiteSpace(rId, nameof(rId));

            string normalized = StringHelper.Normalize(rId);
            return await _dbContext.Set<StudentSpeciality>()
                .AnyAsync(p => speciality.Equals(p.Speciality) 
                               && p.Student!.RId == normalized && p.Student.DeletedAt == null);
        }
        
        public async Task<bool> SpecialityContainsAsync(Speciality speciality, Student student)
        {
            Asserts.NotNull(speciality, nameof(speciality));
            Asserts.NotNull(student, nameof(student));
            
            return await _dbContext.Set<StudentSpeciality>()
                .AnyAsync(p => speciality.Id  == p.SpecialityId 
                               && student.Id == p.StudentId 
                               && p.DeletedAt == null);
        }
        
        public async Task<Event> DeleteSpeciality(StudentSpeciality studentSpeciality, User user)
        {
            Asserts.NotNull(studentSpeciality, nameof(studentSpeciality));
            Asserts.NotNull(studentSpeciality.Speciality, nameof(studentSpeciality.Speciality));
            Asserts.NotNull(studentSpeciality.Student, nameof(studentSpeciality.Student));
            Asserts.NotNull(studentSpeciality.Student!.Space, nameof(studentSpeciality.Student.Space));
            _dbContext.Remove(studentSpeciality);
            await _dbContext.SaveChangesAsync();
            
            var publisherIds = new List<string>
            {
                studentSpeciality.Student!.PublisherId,
                studentSpeciality.Student.Space.PublisherId,
                studentSpeciality.Speciality!.PublisherId
            };

            return await _eventService.EmitAsync(publisherIds, user.ActorId, "STUDENT_SPECIALITY_DELETE", studentSpeciality);
        }
    }
}