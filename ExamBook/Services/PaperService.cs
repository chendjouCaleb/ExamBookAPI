using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ExamBook.Entities;
using ExamBook.Exceptions;
using ExamBook.Identity.Entities;
using ExamBook.Models;
using ExamBook.Models.Data;
using ExamBook.Persistence;
using ExamBook.Utils;
using Microsoft.EntityFrameworkCore;
using Traceability.Models;
using Traceability.Services;

namespace ExamBook.Services
{
    public class PaperService
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly PublisherService _publisherService;
        private readonly EventService _eventService;

        public PaperService(ApplicationDbContext dbContext, 
            EventService eventService, 
            PublisherService publisherService)
        {
            _dbContext = dbContext;
            _eventService = eventService;
            _publisherService = publisherService;
        }

        public async Task<Paper> GetByIdAsync(ulong paperId)
        {
            var paper = await _dbContext.Set<Paper>()
                .Include(p => p.PaperScore)
                .Include(p => p.Participant)
                .Include(p => p.Student)
                .Include(p => p.Test)
                .Include(p => p.TestSpeciality)
                .Where(p => p.Id == paperId)
                .FirstOrDefaultAsync();

            if (paper == null)
            {
                throw new ElementNotFoundException("PaperNotFoundById", paperId);
            }

            return paper;
        }
        
        public async Task<bool> ContainsAsync(Test test, Participant participant)
        {
            AssertHelper.NotNull(test, nameof(test));
            AssertHelper.NotNull(participant, nameof(participant));
            return await _dbContext.Set<Paper>()
                .AnyAsync(p => test.Equals(p.Test) && participant.Equals(p.Participant));
        }
        
        
        public async Task<bool> ContainsAsync(Test test, Student student)
        {
            AssertHelper.NotNull(test, nameof(test));
            AssertHelper.NotNull(student, nameof(student));

            return await _dbContext.Set<Paper>()
                .AnyAsync(p => p.TestId == test.Id && p.StudentId == student.Id);
        }
        
        public async Task<List<Paper>> ContainsAsync(Test test, ICollection<Student> students)
        {
            var studentIds = students.Select(s => s.Id).ToList();

            var duplicates = await _dbContext.Set<Paper>()
                .Where(p => p.TestId == test.Id && p.StudentId != null && studentIds.Contains(p.StudentId ?? 0))
                .ToListAsync();

            return duplicates;
        }
        
        public async Task<List<Paper>> ContainsAsync(Test test, ICollection<Participant> participants)
        {
            var participantIds = participants.Select(s => s.Id).ToList();

            var duplicates = await _dbContext.Set<Paper>()
                .Where(p => p.TestId == test.Id && p.ParticipantId != null && participantIds.Contains(p.ParticipantId ?? 0))
                .ToListAsync();

            return duplicates;
        }
        
        
        public async Task<ActionResultModel<List<Paper>>> AddStudentTestPapersAsync(Test test, User user)
        {
            AssertHelper.IsTrue(test.ExaminationId == null);
			
            var papers = await _dbContext.Papers
                .Where(p => p.TestId == test.Id)
                .Select(p => new { p.StudentId, p.ParticipantId })
                .ToListAsync();

            var studentQuery = _dbContext.Students
                .Where(s => s.SpaceId == test.SpaceId);

            var students = await studentQuery.ToListAsync();

            var selectedStudents = students
                .Where(s => papers.All(p => p.StudentId != s.Id))
                .ToList();

            return await AddTestPapersAsync(test, selectedStudents, user);
        }

        
        public async Task<ActionResultModel<List<Paper>>> AddExaminationTestPapers(Test test, User user)
        {
            AssertHelper.IsTrue(test.ExaminationId != null);
			
            var papers = await _dbContext.Papers
                .Where(p => p.TestId == test.Id)
                .Select(p => new { p.StudentId, p.ParticipantId })
                .ToListAsync();
            var specialities = test.TestSpecialities;

            IQueryable<Participant> query = _dbContext.Participants
                .Where(p => p.ExaminationId == test.ExaminationId);

            if (test.IsSpecialized)
            {
                query = query.Where(p => p.ParticipantSpecialities
                    .Any(ps => specialities
                        .Any(ts => ps.ExaminationSpecialityId == ts.ExaminationSpecialityId)));
            }

            var participants = await query.ToListAsync();
            var selectedParticipants = participants
                .SkipWhile(p => papers.Any(paper => paper.ParticipantId == p.Id))
                .ToList();
				
            return await AddTestPapersAsync(test, selectedParticipants, user);
        }

        public async Task<ActionResultModel<List<Paper>>> AddTestPapersAsync(Test test, List<Student> students, User adminUser)
        {
            AssertHelper.NotNull(test, nameof(test));
            AssertHelper.NotNull(test.Space, nameof(test.Space));
            AssertHelper.NotNull(adminUser, nameof(adminUser));
            AssertHelper.NotNull(students, nameof(students));
            AssertHelper.IsTrue(students.TrueForAll(s => s.SpaceId == test.SpaceId));

            var papers = new List<Paper>();

            var duplicata = await ContainsAsync(test, students);
            if (duplicata.Count > 0)
            {
                throw new DuplicateValueException("PaperStudentExists", duplicata);
            }

            foreach (var student in students)
            {
                var paper = await CreatePaperAsync(test);
                paper.Student = student;
                papers.Add(paper);
            }

            var publishers = papers.Select(p => p.Publisher!).ToList();
                        
            
            await _dbContext.AddRangeAsync(papers);
            await _dbContext.SaveChangesAsync();
            await _publisherService.SaveAllAsync(publishers);

            var publisherIds = new List<string> {test.PublisherId, test.Space.PublisherId };
            publisherIds.AddRange(students.Select(s => s.PublisherId));
            if (test.CourseId != null)
            {
                AssertHelper.NotNull(test.Course, nameof(test.Course));
                publisherIds.Add(test.Course!.PublisherId);
            }

            var paperIds = papers.Select(p => p.Id).ToList();
            var actionData = new {PaperIds = paperIds};
            var action = await _eventService.EmitAsync(publisherIds, adminUser.ActorId, "PAPERS_ADD", actionData);
            return new ActionResultModel<List<Paper>>(papers, action);
        }

        
        public async Task<ActionResultModel<List<Paper>>> AddTestPapersAsync(Test test, 
            List<Participant> participants, 
            User adminUser)
        {
            AssertHelper.NotNull(test, nameof(test));
            AssertHelper.NotNull(test.Space, nameof(test.Space));
            AssertHelper.NotNull(adminUser, nameof(adminUser));
            AssertHelper.NotNull(participants, nameof(participants));
            AssertHelper.IsTrue(participants.TrueForAll(s => s.ExaminationId == test.ExaminationId));

            var papers = new List<Paper>();

            var duplicates = await ContainsAsync(test, participants);
            if (duplicates.Count > 0)
            {
                throw new DuplicateValueException("PaperParticipantsExists", duplicates);
            }

            foreach (var participant in participants)
            {
                var paper = await CreatePaperAsync(test);
                paper.Participant = participant;
            }

            var publishers = papers.Select(p => p.Publisher!).ToList();
                        
            
            await _dbContext.AddRangeAsync(papers);
            await _dbContext.SaveChangesAsync();
            await _publisherService.SaveAllAsync(publishers);

            var publisherIds = new List<string> {test.PublisherId, test.Space.PublisherId };
            publisherIds.AddRange(participants.Select(s => s.PublisherId));
            if (test.CourseId != null)
            {
                AssertHelper.NotNull(test.Course, nameof(test.Course));
                publisherIds.Add(test.Course!.PublisherId);
            }

            var paperIds = papers.Select(p => p.Id).ToList();
            var actionData = new {PaperIds = paperIds};
            var action = await _eventService.EmitAsync(publisherIds, adminUser.Id, "PAPERS_ADD", actionData);
            return new ActionResultModel<List<Paper>>(papers, action);
        }


        public async Task<Event> SetScoreAsync(Test test, List<PaperScoreModel> models, User adminUser)
        {
            AssertHelper.NotNull(test, nameof(test));
            AssertHelper.NotNull(models, nameof(models));

            var paperIds = models.Select(m => m.PaperId).ToList();
            var papers = await _dbContext.Papers
                .Include(p => p.PaperScore)
                .Include(p => p.Student)
                .Where(p => paperIds.Contains(p.Id))
                .ToListAsync();

            var changeDataList = new List<ChangeScoreData>();
            foreach (var model in models)
            {
                var paper = papers.Find(p => p.Id == model.PaperId)!;
                var score = paper.PaperScore;

                var data = new ChangeScoreData
                {
                    PaperId = paper.Id,
                    Last = score.Value,
                    Current = model.Score
                };
                changeDataList.Add(data);
                score.Value = model.Score;
            }
            
            _dbContext.UpdateRange(papers.Select(p => p.PaperScore));
            await _dbContext.SaveChangesAsync();
            

            var publisherIds = new List<string> {test.PublisherId, test.Space.PublisherId};
            publisherIds.AddRange(papers.Select(p => p.PublisherId));
            publisherIds.AddRange(papers.Select(p => p.Student!.PublisherId));

            if (test.Course != null)
            {
                publisherIds.Add(test.Course.PublisherId);
            }

            if (test.Examination != null)
            {
                publisherIds.Add(test.Examination.PublisherId);
            }

            _dbContext.UpdateRange(papers);
            await _dbContext.SaveChangesAsync();
            return await _eventService.EmitAsync(publisherIds, adminUser.ActorId, "PAPER_SET_SCORES", changeDataList);
        }

        public async Task<Paper> AddPaperAsync(Test test, Student student)
        {
            AssertHelper.NotNull(test, nameof(test));
            AssertHelper.NotNull(student, nameof(student));
            
            var paper = await CreatePaperAsync(test);
            paper.Student = student;

            await _dbContext.AddAsync(paper);
            await _dbContext.AddAsync(paper.PaperScore!);
            await _dbContext.SaveChangesAsync();
            await _publisherService.SaveAsync(paper.Publisher!);
            return paper;
        }
        
        public async Task<Paper> AddPaperAsync(Test test, Participant participant)
        {
            AssertHelper.NotNull(test, nameof(test));
            AssertHelper.NotNull(participant, nameof(participant));
            
            var paper = await CreatePaperAsync(test);
            paper.Participant = participant;

            if (participant.Student != null)
            {
                paper.Student = participant.Student;
            }

            await _dbContext.AddAsync(paper);
            await _dbContext.AddAsync(paper.PaperScore!);
            await _dbContext.SaveChangesAsync();
            await _publisherService.SaveAsync(paper.Publisher!);
            return paper;
        }
        
        public async Task<Paper> CreatePaperAsync(Test test)
        {
            AssertHelper.NotNull(test, nameof(test));

            var publisher = _publisherService.Create();
            var score = new PaperScore();
            Paper paper = new()
            {
                Test = test,
                PaperScore = score,
                Publisher = publisher,
                PublisherId = publisher.Id
            };
            return paper;
        }

        
        


        public async Task DeletePaper(Paper paper)
        {
            AssertHelper.NotNull(paper, nameof(paper));
            _dbContext.Remove(paper);
            await _dbContext.SaveChangesAsync();
        }

    
    }
}