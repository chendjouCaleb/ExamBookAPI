using System;
using System.Linq;
using System.Threading.Tasks;
using ExamBook.Entities;
using ExamBook.Helpers;
using ExamBook.Models;
using ExamBook.Utils;
using Microsoft.EntityFrameworkCore;

namespace ExamBook.Services
{
    public class ParticipantService
    {
        private readonly DbContext _dbContext;

        public ParticipantService(DbContext dbContext)
        {
            _dbContext = dbContext;
        }


        public async Task<Participant> Add(Examination examination, ParticipantAddModel model)
        {
            Asserts.NotNull(examination, nameof(examination));
            Asserts.NotNull(model, nameof(model));

            if (await ContainsAsync(examination, model.RId))
            {
                ParticipantHelper.ThrowDuplicateRId(examination, model.RId);
            }
            string normalizedRid = model.RId.Normalize().ToUpper();
            Participant participant = new()
            {
                FirstName = model.FirstName,
                LastName = model.LastName,
                BirthDate = model.BirthDate,
                Sex = model.Sex,
                NormalizedRId = normalizedRid,
                RId = model.RId,
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

        public async Task<ParticipantSpeciality> AddSpecialityAsync(Participant participant,
            ExaminationSpeciality examinationSpeciality)
        {
            var participantSpeciality = await _AddSpecialityAsync(participant, examinationSpeciality);
            await _dbContext.AddAsync(participantSpeciality);
            await _dbContext.SaveChangesAsync();
            return participantSpeciality;
        }

        public async Task ChangeRId(Participant participant, ParticipantChangeRIdModel model)
        {
            Asserts.NotNull(participant, nameof(participant));
            Asserts.NotNull(participant.Examination, nameof(participant.Examination));
            Asserts.NotNull(model, nameof(model));
            if (await ContainsAsync(participant.Examination, model.RId))
            {
                ParticipantHelper.ThrowDuplicateRId(participant.Examination, model.RId);
            }
            string normalizedRid = model.RId.Normalize().ToUpper();
            participant.RId = model.RId;
            participant.NormalizedRId = normalizedRid;
            _dbContext.Update(participant);
            await _dbContext.SaveChangesAsync();
        }


        public async Task ChangeInfo(Participant participant, ParticipantChangeInfoModel model)
        {
            Asserts.NotNull(participant, nameof(participant));
            Asserts.NotNull(participant.Examination, nameof(participant.Examination));
            Asserts.NotNull(model, nameof(model));

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
            Asserts.NotNull(participant, nameof(participant));
            Asserts.NotNull(examinationSpeciality, nameof(examinationSpeciality));
            Asserts.NotNull(examinationSpeciality.Examination, nameof(examinationSpeciality.Examination));

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


        public async Task<bool> ContainsAsync(Examination examination, string rId)
        {
            Asserts.NotNull(examination, nameof(examination));
            Asserts.NotNullOrWhiteSpace(rId, nameof(rId));

            string normalized = rId.Normalize().ToUpper();
            return await _dbContext.Set<Participant>()
                .AnyAsync(p => examination.Equals(p.Examination) && p.RId == normalized);
        }
        
        
        public async Task<bool> SpecialityContainsAsync(ExaminationSpeciality examinationSpeciality, string rId)
        {
            Asserts.NotNull(examinationSpeciality, nameof(examinationSpeciality));
            Asserts.NotNullOrWhiteSpace(rId, nameof(rId));

            string normalized = rId.Normalize().ToUpper();
            return await _dbContext.Set<ParticipantSpeciality>()
                .AnyAsync(p => examinationSpeciality.Equals(p.ExaminationSpeciality) 
                               && p.Participant.RId == normalized);
        }
        
        public async Task<bool> SpecialityContainsAsync(ExaminationSpeciality examinationSpeciality, Participant participant)
        {
            Asserts.NotNull(examinationSpeciality, nameof(examinationSpeciality));
            Asserts.NotNull(participant, nameof(participant));
            
            return await _dbContext.Set<ParticipantSpeciality>()
                .AnyAsync(p => examinationSpeciality.Equals(p.ExaminationSpeciality) 
                               && participant.Equals(p.ParticipantId));
        }

        public async Task<Participant?> FindAsync(Examination examination, string rId)
        {
            Asserts.NotNull(examination, nameof(examination));
            Asserts.NotNullOrWhiteSpace(rId, nameof(rId));

            string normalized = rId.Normalize().ToUpper();
            var participant = await _dbContext.Set<Participant>()
                .FirstOrDefaultAsync(p => examination.Equals(p.Examination) && p.RId == normalized);

            if (participant == null)
            {
                ParticipantHelper.ThrowParticipantNotFound(examination, rId);
            }

            return participant;
        }

        
        public async Task DeleteSpeciality(ParticipantSpeciality participantSpeciality)
        {
            Asserts.NotNull(participantSpeciality, nameof(participantSpeciality));
            _dbContext.Remove(participantSpeciality);
            await _dbContext.SaveChangesAsync();
        }


        public async Task MarkAsDeleted(Participant participant)
        {
            Asserts.NotNull(participant, nameof(participant));
            participant.Sex = '0';
            participant.BirthDate = DateTime.MinValue;
            participant.FirstName = "";
            participant.LastName = "";
            participant.RId = "";
            participant.NormalizedRId = "";
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