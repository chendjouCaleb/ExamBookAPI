using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ExamBook.Entities;
using ExamBook.Helpers;
using ExamBook.Utils;
using Microsoft.EntityFrameworkCore;
using Vx.Services;

namespace ExamBook.Services
{
    public class PaperService
    {
        private readonly DbContext _dbContext;
        private readonly PublisherService _publisherService;
        private readonly EventService _eventService;

        public PaperService(DbContext dbContext, EventService eventService, PublisherService publisherService)
        {
            _dbContext = dbContext;
            _eventService = eventService;
            _publisherService = publisherService;
        }


        public async Task<List<Paper>> AddTestPapers(Test test)
        {
            var papers = await CreateTestPapers(test);
            await _dbContext.AddRangeAsync(papers);
            await _dbContext.SaveChangesAsync();
            return papers;
        }

        public async Task<List<Paper>> CreateTestPapers(Test test)
        {
            Asserts.NotNull(test, nameof(test));
            
            var participants = _dbContext.Set<Participant>()
                .Where(p => test.Examination.Equals(p.Examination));

            var papers = new List<Paper>();
            foreach (var participant in participants)
            {
                if (!await ContainsAsync(test, participant))
                {
                    var paper = await CreatePaperAsync(test, participant);
                    papers.Add(paper);
                }
            }

            return papers;
        }

        public async Task<Paper> AddPaperAsync(Test test, Participant participant)
        {
            var paper = await CreatePaperAsync(test, participant);

            await _dbContext.AddAsync(paper);
            await _dbContext.AddRangeAsync(paper.PaperSpecialities);
            await _dbContext.SaveChangesAsync();
            return paper;
        }
        
        public async Task<Paper> CreatePaperAsync(Test test, Participant participant)
        {
            Asserts.NotNull(test, nameof(test));
            Asserts.NotNull(participant, nameof(participant));

            if (await ContainsAsync(test, participant))
            {
                PaperHelper.ThrowDuplicatePaper(test, participant);
            }
            
            Paper paper = new()
            {
                Test = test,
                Participant = participant,
                ParticipantId = participant.Id
            };
            paper.PaperSpecialities = await CreatePaperSpecialitiesAsync(paper);
            return paper;
        }
        

        public async Task<PaperSpeciality> AddPaperSpecialityAsync(Paper paper, ParticipantSpeciality participantSpeciality)
        {
            var paperSpeciality = await CreatePaperSpecialityAsync(paper, participantSpeciality);
            await _dbContext.AddAsync(paperSpeciality);
            await _dbContext.SaveChangesAsync();
            return paperSpeciality;
        }

        public async Task<List<PaperSpeciality>> AddPaperSpecialitiesAsync(Paper paper)
        {
            var paperSpecialities = await CreatePaperSpecialitiesAsync(paper);
            await _dbContext.AddRangeAsync(paperSpecialities);
            await _dbContext.SaveChangesAsync();
            return paperSpecialities;
        }
        
        public async Task<List<PaperSpeciality>> CreatePaperSpecialitiesAsync(Paper paper)
        {
            var participantSpecialities = await _dbContext.Set<ParticipantSpeciality>()
                .Where(ps => paper.ParticipantId == ps.ParticipantId)
                .ToListAsync();

            var paperSpecialities = new List<PaperSpeciality>();
            foreach (var participantSpeciality in participantSpecialities)
            {
                if (!(await ContainsPaperSpeciality(paper, participantSpeciality)))
                {
                    var paperSpeciality = await CreatePaperSpecialityAsync(paper, participantSpeciality);
                    paperSpecialities.Add(paperSpeciality);
                }
            }

            return paperSpecialities;
        }

        public async Task<PaperSpeciality> CreatePaperSpecialityAsync(Paper paper,
            ParticipantSpeciality participantSpeciality)
        {
            Asserts.NotNull(paper, nameof(paper));
            Asserts.NotNull(participantSpeciality, nameof(participantSpeciality));

            if (await ContainsPaperSpeciality(paper, participantSpeciality))
            {
                PaperHelper.ThrowDuplicatePaperSpeciality();
            }

            return new(paper, participantSpeciality);
        }

        public async Task<bool> ContainsAsync(Test test, Participant participant)
        {
            return await _dbContext.Set<Paper>()
                .AnyAsync(p => test.Equals(p.Test) && participant.Equals(p.Participant));
        }

        public async Task<bool> ContainsPaperSpeciality(Paper paper, ParticipantSpeciality participantSpeciality)
        {
            return await _dbContext.Set<PaperSpeciality>()
                .AnyAsync(ps => paper.Equals(ps.Paper) && participantSpeciality.Equals(ps.ParticipantSpeciality));
        }


        public async Task DeletePaper(Paper paper)
        {
            Asserts.NotNull(paper, nameof(paper));
            var paperSpecialities = _dbContext.Set<PaperSpeciality>()
                .Where(p => paper.Equals(p.Paper));
            
            _dbContext.RemoveRange(paperSpecialities);
            _dbContext.Remove(paper);
            await _dbContext.SaveChangesAsync();
        }

        public async Task DeletePaperSpeciality(PaperSpeciality paperSpeciality)
        {
            Asserts.NotNull(paperSpeciality, nameof(paperSpeciality));
            _dbContext.Remove(paperSpeciality);
            await _dbContext.SaveChangesAsync();
        }
    }
}