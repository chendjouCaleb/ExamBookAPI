using System;
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
using Microsoft.Extensions.Logging;
using Vx.Models;
using Vx.Services;

namespace ExamBook.Services
{
    public class ParticipantService
    {
        private readonly DbContext _dbContext;
        private readonly PublisherService _publisherService;
        private readonly EventService _eventService;
        private readonly ILogger<ParticipantService> _logger;

        public ParticipantService(DbContext dbContext, PublisherService publisherService, EventService eventService, ILogger<ParticipantService> logger)
        {
            _dbContext = dbContext;
            _publisherService = publisherService;
            _eventService = eventService;
            _logger = logger;
        }

        public async Task<Participant> GetByIdAsync(ulong participantId)
        {
            
        }

        public async Task<Participant> GetByCodeAsync(Examination examination, string code)
        {
            
        }
        
        
        public async Task<bool> ContainsByCodeAsync(Examination examination, string code)
        {
            string normalizedCode = StringHelper.Normalize(code);
            return await _dbContext.Set<Participant>()
                .Where(p => p.ExaminationId == examination.Id && p.NormalizedCode == normalizedCode)
                .AnyAsync();
        }


        public async Task<ActionResultModel<Participant>> AddAsync(Examination examination, 
            User user,
            ParticipantAddModel model,
            User actor)
        {
            AssertHelper.NotNull(examination, nameof(examination));
            AssertHelper.NotNull(model, nameof(model));

            if (await ContainsAsync(examination, model.Code))
            {
                ParticipantHelper.ThrowDuplicateCode(examination, model.Code);
            }
            string normalizedCode = model.Code.Normalize().ToUpper();
            Participant participant = new()
            {
                FirstName = model.FirstName,
                LastName = model.LastName,
                BirthDate = model.BirthDate,
                Sex = model.Sex,
                NormalizedCode = normalizedCode,
                Code = model.Code,
                Examination = examination
            };
            await _dbContext.AddAsync(participant);

            var examinationSpecialities = _dbContext.Set<ExaminationSpeciality>()
                .Where(e => model.ExaminationSpecialityIds.Contains(e.Id))
                .ToList();

            foreach (var examinationSpeciality in examinationSpecialities)
            {
                var participantSpeciality = await _AddSpecialityAsync(participant, examinationSpeciality);
                await _dbContext.AddAsync(participantSpeciality);
            }
            
            await _dbContext.SaveChangesAsync();
            return participant;
        }

        public async Task<ICollection<Participant>> AddParticipants(Examination examination,
            ICollection<Student> students)
        {
            AssertHelper.NotNull(examination, nameof(examination));
            AssertHelper.NotNull(examination.Space, nameof(examination.Space));
            AssertHelper.NotNull(students, nameof(students));
            
            students = students.DistinctBy(s => s.Id).ToList();
            var studentIds = students.Select(s => s.Id ).ToList();
            var contains = await _dbContext.Set<Participant>()
                .Where(p => p.StudentId != null && studentIds.Contains(p.StudentId ?? 0) && p.ExaminationId == examination.Id)
                .AnyAsync();

            if (contains)
            {
                throw new IllegalOperationException("StudentExaminationsExists");
            }


            throw new NotImplementedException("Method not terminated");
        }
        public async Task<Participant> CreateParticipant(Examination examination, Student student)
        {
            AssertHelper.NotNull(examination, nameof(examination));
            AssertHelper.NotNull(examination.Space, nameof(examination.Space));
            AssertHelper.NotNull(student, nameof(student));
            AssertHelper.NotNull(student.Space, nameof(student.Space));

            if (await ContainsStudentAsync(examination, student))
            {
                throw new IllegalOperationException("ExaminationStudentExists{0}", student.Id);
            }

            return new Participant
            {
                Examination = examination,
                Student = student
            };
        }


        public async Task<bool> ContainsStudentAsync(Examination examination, Student student)
        {
            AssertHelper.NotNull(examination, nameof(examination));
            AssertHelper.NotNull(student.Space, nameof(student.Space));

            return await _dbContext.Set<Participant>()
                .Where(p => p.ExaminationId == examination.Id && p.StudentId == student.Id)
                .AnyAsync();
        }

        public async Task<ParticipantSpeciality> AddSpecialityAsync(Participant participant,
            ExaminationSpeciality examinationSpeciality)
        {
            var participantSpeciality = await _AddSpecialityAsync(participant, examinationSpeciality);
            await _dbContext.AddAsync(participantSpeciality);
            await _dbContext.SaveChangesAsync();
            return participantSpeciality;
        }

        public async Task<Event> ChangeCodeAsync(Participant participant, string code, User actor)
        {
            AssertHelper.NotNull(participant, nameof(participant));
            AssertHelper.NotNull(participant.Examination, nameof(participant.Examination));
           
            if (await ContainsAsync(participant.Examination, code))
            {
                ParticipantHelper.ThrowDuplicateCode(participant.Examination, model.Code);
            }
            string normalizedCode = model.Code.Normalize().ToUpper();
            participant.Code = model.Code;
            participant.NormalizedCode = normalizedCode;
            _dbContext.Update(participant);
            await _dbContext.SaveChangesAsync();
        }

        public async Task<Event> ChangeNameAsync(Participant participant, ChangeNameModel model, User actor)
        {
        }

        public async Task<Event> ChangeSexAsync(Participant participant, char sex, User actor)
        {
        }

        public async Task<Event> ChangeBirthDateAsync(Participant participant, DateOnly birthDate, User actor)
        {
        }


        public async Task ChangeInfoAsync(Participant participant, ParticipantChangeInfoModel model)
        {
            AssertHelper.NotNull(participant, nameof(participant));
            AssertHelper.NotNull(participant.Examination, nameof(participant.Examination));
            AssertHelper.NotNull(model, nameof(model));

            participant.Sex = model.Sex;
            participant.BirthDate = model.BirthDate;
            participant.FirstName = model.FirstName;
            participant.LastName = model.LastName;
            _dbContext.Update(participant);
            await _dbContext.SaveChangesAsync();
        }
        
        private async Task<ParticipantSpeciality> _AddSpecialityAsync(
            Participant participant,
            ExaminationSpeciality examinationSpeciality)
        {
            AssertHelper.NotNull(participant, nameof(participant));
            AssertHelper.NotNull(examinationSpeciality, nameof(examinationSpeciality));
            AssertHelper.NotNull(examinationSpeciality.Examination, nameof(examinationSpeciality.Examination));

            if (examinationSpeciality.ExaminationId != participant.ExaminationId)
            {
                throw new InvalidOperationException("Incompatible entities.");
            }

            if (await SpecialityContainsAsync(examinationSpeciality, participant))
            {
                ParticipantHelper.ThrowDuplicateParticipantSpeciality(examinationSpeciality, participant);
            }

            ParticipantSpeciality participantSpeciality = new()
            {
                Participant = participant,
                ExaminationSpeciality = examinationSpeciality
            };
            return participantSpeciality;
        }


        public async Task<bool> ContainsAsync(Examination examination, string code)
        {
            AssertHelper.NotNull(examination, nameof(examination));
            AssertHelper.NotNullOrWhiteSpace(code, nameof(code));

            string normalized = code.Normalize().ToUpper();
            return await _dbContext.Set<Participant>()
                .AnyAsync(p => examination.Equals(p.Examination) && p.Code == normalized);
        }
        
        
        public async Task<bool> SpecialityContainsAsync(ExaminationSpeciality examinationSpeciality, string code)
        {
            AssertHelper.NotNull(examinationSpeciality, nameof(examinationSpeciality));
            AssertHelper.NotNullOrWhiteSpace(code, nameof(code));

            string normalized = code.Normalize().ToUpper();
            return await _dbContext.Set<ParticipantSpeciality>()
                .AnyAsync(p => examinationSpeciality.Equals(p.ExaminationSpeciality) 
                               && p.Participant.Code == normalized);
        }
        
        public async Task<bool> SpecialityContainsAsync(ExaminationSpeciality examinationSpeciality, Participant participant)
        {
            AssertHelper.NotNull(examinationSpeciality, nameof(examinationSpeciality));
            AssertHelper.NotNull(participant, nameof(participant));
            
            return await _dbContext.Set<ParticipantSpeciality>()
                .AnyAsync(p => examinationSpeciality.Equals(p.ExaminationSpeciality) 
                               && participant.Equals(p.ParticipantId));
        }

        public async Task<Participant?> FindAsync(Examination examination, string code)
        {
            AssertHelper.NotNull(examination, nameof(examination));
            AssertHelper.NotNullOrWhiteSpace(code, nameof(code));

            string normalized = code.Normalize().ToUpper();
            var participant = await _dbContext.Set<Participant>()
                .FirstOrDefaultAsync(p => examination.Equals(p.Examination) && p.Code == normalized);

            if (participant == null)
            {
                ParticipantHelper.ThrowParticipantNotFound(examination, code);
            }

            return participant;
        }

        
        public async Task DeleteSpeciality(ParticipantSpeciality participantSpeciality)
        {
            AssertHelper.NotNull(participantSpeciality, nameof(participantSpeciality));
            _dbContext.Remove(participantSpeciality);
            await _dbContext.SaveChangesAsync();
        }


        public async Task MarkAsDeleted(Participant participant)
        {
            AssertHelper.NotNull(participant, nameof(participant));
            participant.Sex = '0';
            participant.BirthDate = DateOnly.MinValue;
            participant.FirstName = "";
            participant.LastName = "";
            participant.Code = "";
            participant.NormalizedCode = "";
            participant.DeletedAt = DateTime.Now;
            _dbContext.Update(participant);
            await _dbContext.SaveChangesAsync();
        }

        public async Task Delete(Participant participant)
        {
           var participantSpecialities = await _dbContext.Set<ParticipantSpeciality>()
                .Where(p => participant.Equals(p.ParticipantId))
                .ToListAsync();
           
           _dbContext.Set<ParticipantSpeciality>().RemoveRange(participantSpecialities);
           _dbContext.Set<Participant>().Remove(participant);
           await _dbContext.SaveChangesAsync();
        }
    }
}