﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ExamBook.Entities;
using ExamBook.Exceptions;
using ExamBook.Helpers;
using ExamBook.Identity.Entities;
using ExamBook.Models;
using ExamBook.Utils;
using Microsoft.EntityFrameworkCore;
using Traceability.Models;
using Traceability.Services;

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
            AssertHelper.NotNull(student, nameof(student));
            AssertHelper.NotNull(student.Space, nameof(student.Space));
            AssertHelper.NotNull(speciality, nameof(speciality));
            AssertHelper.NotNull(user, nameof(user));

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

        public async Task<ActionResultModel<ICollection<StudentSpeciality>>> AddSpecialitiesAsync(
            Student student, HashSet<ulong> specialityIds, User user)
        {
            var specialities = _dbContext.Set<Speciality>()
                .Where(e => specialityIds.Contains(e.Id))
                .ToList();

            return await AddSpecialitiesAsync(student, specialities, user);
        }

        public async Task<ActionResultModel<ICollection<StudentSpeciality>>> AddSpecialitiesAsync(
            Student student, ICollection<Speciality> specialities, User user)
        {
            AssertHelper.NotNull(student, nameof(student));
            AssertHelper.NotNull(student.Space, nameof(student.Space));
            AssertHelper.NotNull(specialities, nameof(specialities));
            AssertHelper.NotNull(user, nameof(user));


            var studentSpecialities = new List<StudentSpeciality>();
            foreach (var speciality in specialities)
            {
                var studentSpeciality = await CreateSpecialityAsync(student, speciality);
                await _dbContext.AddAsync(studentSpeciality);
                studentSpecialities.Add(studentSpeciality);
            }

            var publisherIds = new List<string> {student.Space.PublisherId, student.PublisherId};
            publisherIds.AddRange(specialities.Select(s => s.PublisherId));
            var @event = await _eventService.EmitAsync(publisherIds, user.ActorId, "STUDENT_SPECIALITIES_ADD", studentSpecialities);

            return new ActionResultModel<ICollection<StudentSpeciality>>(studentSpecialities, @event);
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
            AssertHelper.NotNull(student, nameof(student));
            AssertHelper.NotNull(speciality, nameof(speciality));

            if (speciality.SpaceId != student.SpaceId)
            {
                throw new InvalidOperationException("Incompatible entities.");
            }

            if (await ContainsAsync(student, speciality))
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
            AssertHelper.NotNull(speciality, nameof(speciality));
            AssertHelper.NotNullOrWhiteSpace(rId, nameof(rId));

            string normalized = StringHelper.Normalize(rId);
            return await _dbContext.Set<StudentSpeciality>()
                .AnyAsync(p => speciality.Equals(p.Speciality) 
                               && p.Student!.Code == normalized && p.Student.DeletedAt == null);
        }
        
        public async Task<bool> ContainsAsync(Student student, Speciality speciality)
        {
            AssertHelper.NotNull(speciality, nameof(speciality));
            AssertHelper.NotNull(student, nameof(student));
            
            return await _dbContext.Set<StudentSpeciality>()
                .AnyAsync(p => speciality.Id  == p.SpecialityId 
                               && student.Id == p.StudentId 
                               && p.DeletedAt == null);
        }
        
        public async Task<Event> DeleteSpeciality(StudentSpeciality studentSpeciality, User user)
        {
            AssertHelper.NotNull(studentSpeciality, nameof(studentSpeciality));
            AssertHelper.NotNull(studentSpeciality.Speciality, nameof(studentSpeciality.Speciality));
            AssertHelper.NotNull(studentSpeciality.Student, nameof(studentSpeciality.Student));
            AssertHelper.NotNull(studentSpeciality.Student!.Space, nameof(studentSpeciality.Student.Space));
            
            studentSpeciality.DeletedAt = DateTime.UtcNow;
            _dbContext.Update(studentSpeciality);
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