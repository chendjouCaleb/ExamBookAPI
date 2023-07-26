using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using ExamBook.Entities;
using ExamBook.Exceptions;
using ExamBook.Helpers;
using ExamBook.Identity.Entities;
using ExamBook.Models;
using ExamBook.Persistence;
using ExamBook.Utils;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Traceability.Services;

namespace ExamBook.Services
{
    public class ParticipantService
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly PublisherService _publisherService;
        private readonly ParticipantSpecialityService _participantSpecialityService;
        private readonly EventService _eventService;
        private readonly ILogger<ParticipantService> _logger;

        public ParticipantService(ApplicationDbContext dbContext, 
            PublisherService publisherService, 
            EventService eventService, 
            ILogger<ParticipantService> logger, ParticipantSpecialityService participantSpecialityService)
        {
            _dbContext = dbContext;
            _publisherService = publisherService;
            _eventService = eventService;
            _logger = logger;
            _participantSpecialityService = participantSpecialityService;
        }

        public async Task<Participant> GetByIdAsync(ulong participantId)
        {
            var participant = await _dbContext.Participants
                .Include(p => p.Examination)
                .Where(p => p.Id == participantId)
                .FirstOrDefaultAsync();

            if (participant == null)
            {
                throw new ElementNotFoundException("ParticipantNotFoundById", participantId);
            }

            return participant;
        }

        public async Task<Participant> GetByCodeAsync(Examination examination, string code)
        {
            var normalizedCode = StringHelper.Normalize(code);
            var participant = await _dbContext.Participants
                .Include(p => p.Examination)
                .Where(p => p.NormalizedCode == normalizedCode)
                .FirstOrDefaultAsync();

            if (participant == null)
            {
                throw new ElementNotFoundException("ParticipantNotFoundByCode", code, examination.Id);
            }

            return participant;
        }
        
        
        public async Task<bool> ContainsByCodeAsync(Examination examination, string code)
        {
            string normalizedCode = StringHelper.Normalize(code);
            return await _dbContext.Set<Participant>()
                .Where(p => p.ExaminationId == examination.Id && p.NormalizedCode == normalizedCode)
                .AnyAsync();
        }

        public async Task<HashSet<Participant>> ContainsAsync(Examination examination, HashSet<Student> students)
        {
            AssertHelper.NotNull(examination, nameof(examination));
            AssertHelper.NotNull(students, nameof(students));

            var studentIds = students.Select(s => s.Id);
            var participants = await _dbContext.Set<Participant>()
                .Where(p => p.ExaminationId == examination.Id)
                .Where(p => studentIds.Contains(p.StudentId ?? 0))
                .ToListAsync();
            return participants.ToHashSet();
        }


        public async Task<ActionResultModel<HashSet<Participant>>> AddAsync(Examination examination,
            HashSet<Student> students, User adminUser)
        {
            AssertHelper.NotNull(examination, nameof(examination));
            AssertHelper.NotNull(students, nameof(students));
            AssertHelper.NotNull(adminUser, nameof(adminUser));
            var examinationSpecialities = await _dbContext.ExaminationSpecialities
                .Include(es => es.Speciality!.Space)
                .Include(es => es.Examination)
                .Where(es => es.ExaminationId == examination.Id)
                .ToListAsync();

            var duplicates = await ContainsAsync(examination, students);
            if (duplicates.Any())
            {
                throw new DuplicateValueException("StudentsAlreadyParticipant", duplicates);
            }

            var participants = new HashSet<Participant>();
            foreach (var student in students)
            {
                AssertHelper.NotNull(student.Specialities, nameof(student.Specialities));
                var specialityIds = student.Specialities.Select(s => s.SpecialityId).ToHashSet();
                var specialities = examinationSpecialities
                    .Where(es => specialityIds.Contains(es.SpecialityId ?? 0))
                    .ToHashSet();

                var participant =  Create(examination, specialities);
                participant.Student = student;
                foreach (var participantSpeciality in participant.ParticipantSpecialities)
                {
                    participantSpeciality.StudentSpeciality = student.Specialities
                        .Where(s => s.Id == participantSpeciality.ExaminationSpeciality.SpecialityId)
                        .First();
                }
                
                participants.Add(participant);
            }

            var publishers = participants.Select(p => p.Publisher!).ToList();

            await _dbContext.AddRangeAsync(participants);
            await _dbContext.SaveChangesAsync();
            await _publisherService.SaveAllAsync(publishers);

            var publisherIds = ImmutableList.Create<string>()
                .AddRange(publishers.Select(p => p.Id))
                .AddRange(students.Select(p => p.PublisherId))
                .AddRange(new[] {examination.Space.PublisherId, examination.PublisherId});
            var eventData = participants.Select(p => p.Id);

            var action = await _eventService.EmitAsync(publisherIds, adminUser.ActorId, "PARTICIPANTS_ADD", eventData);

            return new ActionResultModel<HashSet<Participant>>(participants, action);
        }

        public Participant Create(Examination examination, HashSet<ExaminationSpeciality> examinationSpecialities)
        {
            AssertHelper.NotNull(examination, nameof(examination));
            AssertHelper.NotNull(examinationSpecialities, nameof(examinationSpecialities));
            
            AssertHelper.IsTrue(examinationSpecialities.All(s => s.ExaminationId == examination.Id));

            var publisher = _publisherService.Create();
            Participant participant = new()
            {
                Examination = examination,
                ExaminationId = examination.Id,
                PublisherId = publisher.Id,
                Publisher = publisher
            };

            var participantSpecialities = new List<ParticipantSpeciality>();
            foreach (var examinationSpeciality in examinationSpecialities)
            {
                var participantSpeciality = _participantSpecialityService.Create(participant, examinationSpeciality);
                participantSpecialities.Add(participantSpeciality);
            }

            participant.ParticipantSpecialities = participantSpecialities;

            return participant;
        }

        


        public async Task<bool> ContainsAsync(Examination examination, string code)
        {
            AssertHelper.NotNull(examination, nameof(examination));
            AssertHelper.NotNullOrWhiteSpace(code, nameof(code));

            string normalized = code.Normalize().ToUpper();
            return await _dbContext.Set<Participant>()
                .AnyAsync(p => examination.Equals(p.Examination) && p.Code == normalized);
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


        public async Task MarkAsDeleted(Participant participant)
        {
            AssertHelper.NotNull(participant, nameof(participant));
            participant.Code = "";
            participant.NormalizedCode = "";
            participant.DeletedAt = DateTime.Now;
            _dbContext.Update(participant);
            await _dbContext.SaveChangesAsync();
        }

        public async Task DeleteAsync(Participant participant)
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