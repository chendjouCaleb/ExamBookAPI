using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using ExamBook.Entities;
using ExamBook.Exceptions;
using ExamBook.Identity.Entities;
using ExamBook.Models;
using ExamBook.Persistence;
using ExamBook.Utils;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Traceability.Services;

namespace ExamBook.Services
{
	public class ExaminationStudentService
	{
		private readonly EventService _eventService;
		private readonly ApplicationDbContext _dbContext;
		private readonly PublisherService _publisherService;
		private readonly ParticipantService _participantService;
		private readonly ILogger<ExaminationStudentService> _logger;


		public async Task<ExaminationStudent> GetByIdAsync(ulong examinationStudentId)
		{
			var examinationStudent = await _dbContext.ExaminationStudents
				.Include(es => es.Student.Space)
				.Include(es => es.Participant.Examination)
				.Where(es => es.Id == examinationStudentId)
				.FirstOrDefaultAsync();

			if (examinationStudent == null)
			{
				throw new ElementNotFoundException("ExaminationStudentNotFoundById", examinationStudentId);
			}


			return examinationStudent;
		}


		public async Task<bool> ContainsAsync(Examination examination, Student student)
		{
			AssertHelper.NotNull(examination, nameof(examination));
			AssertHelper.NotNull(student, nameof(student));

			return await _dbContext.ExaminationStudents
				.Where(e => e.Participant.ExaminationId == examination.Id && e.StudentId == student.Id)
				.AnyAsync();
		}
		
		public bool Contains(Examination examination, Student student)
		{
			AssertHelper.NotNull(examination, nameof(examination));
			AssertHelper.NotNull(student, nameof(student));

			return _dbContext.ExaminationStudents
				.Any(e => e.Participant.ExaminationId == examination.Id && e.StudentId == student.Id);
		}

		public async Task<ICollection<Student>> TakeContainsAsync(Examination examination, IList<Student> students)
		{
			AssertHelper.NotNull(examination, nameof(examination));
			AssertHelper.NotNull(students, nameof(students));
			var contains = new List<Student>();
			foreach (var student in students)
			{
				if (await ContainsAsync(examination, student))
				{
					contains.Add(student);
				}
			}

			return contains;
		}


		public async Task<ActionResultModel<ExaminationStudent>> AddAsync(Examination examination, 
			Student student, User actor)
		{
			AssertHelper.NotNull(examination.Space, nameof(examination.Space));
			AssertHelper.NotNull(student, nameof(student));
			AssertHelper.NotNull(actor, nameof(actor));

			AssertHelper.IsTrue(examination.SpaceId == student.SpaceId);
			if (await ContainsAsync(examination, student))
			{
				throw new DuplicateValueException("ExaminationStudentExists", examination.Id, student.Id);
			}

			var examinationSpecialities = await _dbContext.Set<ExaminationSpeciality>()
				.Where(es => es.ExaminationId == examination.Id)
				.ToListAsync();

			var studentSpecialities = examinationSpecialities
				.TakeWhile(es => student.Specialities.Any(s => s.Id == es.SpecialityId))
				.ToList();

			var participant = await _participantService.CreateAsync(examination, studentSpecialities);

			ExaminationStudent examinationStudent = new()
			{
				Student = student,
				Participant = participant
			};
			participant.PublisherId = (await _publisherService.AddAsync()).Id;

			await _dbContext.AddAsync(participant);
			await _dbContext.AddAsync(participant.ParticipantSpecialities);
			await _dbContext.AddAsync(examinationStudent);

			await _dbContext.SaveChangesAsync();

			var publisherIds = new[]
			{
				participant.PublisherId,
				examination.PublisherId,
				examination.Space.PublisherId,
				student.PublisherId
			};

			var action = await _eventService.EmitAsync(publisherIds, actor.ActorId, "EXAMINATION_STUDENT_ADD",
				examinationStudent);

			return new ActionResultModel<ExaminationStudent>(examinationStudent, action);
		}


		public async Task<ActionResultModel<ICollection<ExaminationStudent>>> AddAsync(Examination examination, 
			List<Student> students, User actor) 
		{
			AssertHelper.NotNull(examination, nameof(examination));
			AssertHelper.NotNull(students, nameof(students));
			AssertHelper.IsTrue(students.All(s => s.SpaceId == examination.SpaceId));
			var contains = await TakeContainsAsync(examination, students);

			if (contains != null)
			{
				var studentIds = contains.Select(s => s.Id).ToList();
				throw new DuplicateValueException("ExaminationStudentExists", examination.Id, studentIds);
			}
			
			var examinationSpecialities = await _dbContext.Set<ExaminationSpeciality>()
				.Where(es => es.ExaminationId == examination.Id)
				.ToListAsync();

			var examinationStudents = new HashSet<ExaminationStudent>();
			var participants = new HashSet<Participant>();
			var participantSpecialities = new List<ParticipantSpeciality>();
			foreach (var student in students)
			{
				var studentSpecialities = examinationSpecialities
					.TakeWhile(es => student.Specialities.Any(s => s.Id == es.SpecialityId))
					.ToList();
				var participant = await _participantService.CreateAsync(examination, studentSpecialities);
				var examinationStudent = new ExaminationStudent
				{
					Student = student,
					Participant = participant
				};
				participants.Add(participant);
				examinationStudents.Add(examinationStudent);
				participantSpecialities.AddRange(participant.ParticipantSpecialities);
			}

			await _dbContext.AddRangeAsync(examinationStudents);
			await _dbContext.AddRangeAsync(participantSpecialities);
			await _dbContext.AddRangeAsync(participants);
			await _dbContext.SaveChangesAsync();

			var publishers = participants.Select(p => p.Publisher!).ToList();
			await _publisherService.SaveAll(publishers);

			var examinationSpecialitiesPublisherIds = participantSpecialities
				.Select(ps => ps.ExaminationSpeciality)
				.DistinctBy(es => es.Id)
				.Select(es => es.PublisherId)
				.ToList();

			var publisherIds = ImmutableList.Create<string>()
				.AddRange(publishers.Select(p => p.Id))
				.AddRange(students.Select(s => s.PublisherId))
				.Add(examination.PublisherId)
				.Add(examination.Space.PublisherId)
				.AddRange(examinationSpecialitiesPublisherIds);


			var action = await _eventService.EmitAsync(publisherIds,
				actor.ActorId, 
				"EXAMINATION_STUDENTS_ADD", 
				examinationStudents.Select(es => es.Id).ToList());

			return new ActionResultModel<ICollection<ExaminationStudent>>(examinationStudents, action);
		}


		public async Task DeleteAsync(ExaminationStudent examinationStudent)
		{
			AssertHelper.NotNull(examinationStudent, nameof(examinationStudent));
		}
	}
}