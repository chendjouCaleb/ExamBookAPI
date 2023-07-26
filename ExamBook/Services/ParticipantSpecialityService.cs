using System;
using System.Collections.Generic;
using System.Collections.Immutable;
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
	public class ParticipantSpecialityService
	{
		private readonly ApplicationDbContext _dbContext;
		private readonly PublisherService _publisherService;
		private readonly EventService _eventService;
		private readonly ILogger<ParticipantSpecialityService> _logger;


        public ParticipantSpecialityService(ApplicationDbContext dbContext, PublisherService publisherService,
            EventService eventService, ILogger<ParticipantSpecialityService> logger)
        {
            _dbContext = dbContext;
            _publisherService = publisherService;
            _eventService = eventService;
            _logger = logger;
        }

        public async Task<ParticipantSpeciality> GetByIdAsync(ulong participantSpecialityId)
        {
            var participantSpeciality = await _dbContext.ParticipantSpecialities
                .Include(p => p.Participant.Examination)
                .Include(p => p.ExaminationSpeciality)
                .Where(p => p.Id == participantSpecialityId)
                .FirstOrDefaultAsync();

            if (participantSpeciality == null)
            {
                throw new ElementNotFoundException("ParticipantSpecialityNotFoundById", participantSpecialityId);
            }

            return participantSpeciality;
        }


        public async Task<bool> ContainsAsync(ExaminationSpeciality examinationSpeciality, Participant participant)
        {
            AssertHelper.NotNull(examinationSpeciality, nameof(examinationSpeciality));
            AssertHelper.NotNull(participant, nameof(participant));
            
            return await _dbContext.Set<ParticipantSpeciality>()
                .AnyAsync(p => p.ExaminationSpecialityId == examinationSpeciality.Id 
                               && p.ParticipantId == participant.Id);
        }

      
        public async Task<bool> ContainsAsync(ExaminationSpeciality examinationSpeciality, string code)
        {
            AssertHelper.NotNull(examinationSpeciality, nameof(examinationSpeciality));
            AssertHelper.NotNullOrWhiteSpace(code, nameof(code));

            string normalized = code.Normalize().ToUpper();
            return await _dbContext.Set<ParticipantSpeciality>()
                .AnyAsync(p => examinationSpeciality.Equals(p.ExaminationSpeciality) 
                               && p.Participant.Code == normalized);
        }
      
        
        public async Task<ActionResultModel<HashSet<ParticipantSpeciality>>> AddAsync(Participant participant,
            HashSet<ExaminationSpeciality> examinationSpecialities, User user)
        {
            AssertHelper.NotNull(participant, nameof(participant));
            AssertHelper.NotNull(examinationSpecialities, nameof(examinationSpecialities));
            var specialities = examinationSpecialities.Select(s => s.Speciality).ToList();
            var participantSpecialities = new HashSet<ParticipantSpeciality>();

            foreach (var examinationSpeciality in examinationSpecialities)
            {
                var participantSpeciality = await CreateAsync(participant, examinationSpeciality);
                participantSpecialities.Add(participantSpeciality);
            }
            
            await _dbContext.AddAsync(participantSpecialities);
            await _dbContext.SaveChangesAsync();

            var publisherIds = ImmutableList.Create<string>()
                .AddRange(new[]
                {
                    participant.PublisherId,
                    participant.Student!.PublisherId,
                    participant.Examination.PublisherId,
                    participant.Examination.Space.PublisherId
                })
                .AddRange(examinationSpecialities.Select(es => es.PublisherId))
                .AddRange(specialities.Select(s => s!.PublisherId));

            const string eventName = "PARTICIPANT_SPECIALITIES_ADD";
            var eventData = participantSpecialities.Select(ps => ps.Id).ToList();
            var action = await _eventService.EmitAsync(publisherIds, user.ActorId, eventName, eventData);
            _logger.LogInformation("New Participant specialities: {}", eventData);
            return new ActionResultModel<HashSet<ParticipantSpeciality>>(participantSpecialities, action);
        }

        
        public async Task<ParticipantSpeciality> CreateAsync(Participant participant, 
            ExaminationSpeciality examinationSpeciality)
        {
            AssertHelper.NotNull(participant, nameof(participant));
            AssertHelper.NotNull(examinationSpeciality, nameof(examinationSpeciality));
            AssertHelper.NotNull(examinationSpeciality.Examination, nameof(examinationSpeciality.Examination));

            if (examinationSpeciality.ExaminationId != participant.ExaminationId)
            {
                throw new InvalidOperationException("Incompatible entities.");
            }

            if (await ContainsAsync(examinationSpeciality, participant))
            {
                throw new DuplicateValueException("DuplicateExaminationSpeciality");
            }

            ParticipantSpeciality participantSpeciality = new()
            {
                Participant = participant,
                ExaminationSpeciality = examinationSpeciality
            };
            return participantSpeciality;
        }
        
        
        public ParticipantSpeciality Create(Participant participant, 
            ExaminationSpeciality examinationSpeciality)
        {
            AssertHelper.NotNull(participant, nameof(participant));
            AssertHelper.NotNull(examinationSpeciality, nameof(examinationSpeciality));
            AssertHelper.NotNull(examinationSpeciality.Examination, nameof(examinationSpeciality.Examination));
            
            ParticipantSpeciality participantSpeciality = new()
            {
                Participant = participant,
                ExaminationSpeciality = examinationSpeciality
            };
            return participantSpeciality;
        }
       
        
        
        
        public async Task DeleteSpeciality(ParticipantSpeciality participantSpeciality)
        {
            AssertHelper.NotNull(participantSpeciality, nameof(participantSpeciality));
            _dbContext.Remove(participantSpeciality);
            await _dbContext.SaveChangesAsync();
        }
	}
}