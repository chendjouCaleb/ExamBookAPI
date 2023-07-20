using System;
using System.Collections.Generic;
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
using Traceability.Models;
using Traceability.Services;

namespace ExamBook.Services
{
    public class ParticipantService
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly PublisherService _publisherService;
        private readonly EventService _eventService;
        private readonly ILogger<ParticipantService> _logger;

        public ParticipantService(ApplicationDbContext dbContext, 
            PublisherService publisherService, 
            EventService eventService, 
            ILogger<ParticipantService> logger)
        {
            _dbContext = dbContext;
            _publisherService = publisherService;
            _eventService = eventService;
            _logger = logger;
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

        public async Task<Participant> CreateAsync(Examination examination, List<ExaminationSpeciality> specialities)
        {
            AssertHelper.NotNull(examination, nameof(examination));
            AssertHelper.NotNull(specialities, nameof(specialities));
            
            AssertHelper.IsTrue(specialities.TrueForAll(s => s.ExaminationId == examination.Id));

            Participant participant = new()
            {
                Examination = examination,
                ExaminationId = examination.Id,
                PublisherId = (await _publisherService.CreateAsync()).Id,
            };
            participant.ParticipantSpecialities = specialities
                .Select(s => CreateSpeciality(participant, s))
                .ToList();

            return participant;
        }

        public async Task<ParticipantSpeciality> AddSpecialityAsync(Participant participant,
            ExaminationSpeciality examinationSpeciality)
        {
            var participantSpeciality = await _AddSpecialityAsync(participant, examinationSpeciality);
            await _dbContext.AddAsync(participantSpeciality);
            await _dbContext.SaveChangesAsync();
            return participantSpeciality;
        }

     
        private ParticipantSpeciality CreateSpeciality(Participant participant,
            ExaminationSpeciality examinationSpeciality)
        {
            AssertHelper.NotNull(participant, nameof(participant));
            AssertHelper.NotNull(examinationSpeciality, nameof(examinationSpeciality));
            AssertHelper.NotNull(examinationSpeciality.Examination, nameof(examinationSpeciality.Examination));

            AssertHelper.IsTrue(participant.ExaminationId == examinationSpeciality.ExaminationId);

            ParticipantSpeciality participantSpeciality = new()
            {
                Participant = participant,
                ExaminationSpeciality = examinationSpeciality
            };
            return participantSpeciality;
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