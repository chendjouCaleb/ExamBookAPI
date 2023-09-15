using System.Linq;
using System.Threading.Tasks;
using ExamBook.Entities;
using ExamBook.Exceptions;
using ExamBook.Models;
using ExamBook.Persistence;
using ExamBook.Utils;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Traceability.Services;

namespace ExamBook.Services.Courses
{
	public class CourseClassroomService
	{
		private readonly ApplicationDbContext _dbContext;
		private readonly EventService _eventService;
		private readonly PublisherService _publisherService;
		private readonly SubjectService _subjectService;
		private readonly ILogger<CourseClassroomService> _logger;

		public CourseClassroomService(PublisherService publisherService, 
			SubjectService subjectService, 
			ILogger<CourseClassroomService> logger, 
			EventService eventService, 
			ApplicationDbContext dbContext)
		{
			_publisherService = publisherService;
			_subjectService = subjectService;
			_logger = logger;
			_eventService = eventService;
			_dbContext = dbContext;
		}


		public async Task<CourseClassroom> GetAsync(ulong courseClassroomId)
		{
			var courseClassroom = await _dbContext.CourseClassrooms
				.Include(cc => cc.Classroom.Space)
				.Include(cc => cc.Course)
				.Where(cc => cc.Id == courseClassroomId)
				.FirstOrDefaultAsync();

			if (courseClassroom == null)
			{
				throw new ElementNotFoundException("CourseClassroomNotFoundId", courseClassroomId);
			}

			return courseClassroom;
		}


		public async Task<bool> ContainsAsync(Classroom classroom, Course course)
		{
			return await _dbContext.CourseClassrooms
				.Where(cc => cc.ClassroomId == classroom.Id && cc.CourseId == course.Id)
				.AnyAsync();
		}

		public async Task<ActionResultModel<CourseClassroom>> AddAsync(Classroom classroom, 
			Course course,
			CourseClassroomAddModel model,
			Member member)
		{
			AssertHelper.NotNull(classroom, nameof(classroom));
			AssertHelper.NotNull(course, nameof(course));
			AssertHelper.NotNull(course.Space, nameof(course.Space));
			AssertHelper.NotNull(model, nameof(model));
			AssertHelper.NotNull(member, nameof(member));

			if (await ContainsAsync(classroom, course))
			{
				throw new IllegalOperationException("CourseClassroomExists", course, classroom);
			}

			var subject = _subjectService.Create("COURSE_CLASSROOM_SUBJECT");
			var publisher = _publisherService.Create("COURSE_CLASSROOM_PUBLISHER");

			var courseClassroom = new CourseClassroom
			{
				Course = course,
				Classroom = classroom,
				Code = model.Code,
				Coefficient = model.Coefficient,
				Subject = subject,
				SubjectId = subject.Id,
				Publisher = publisher,
				PublisherId = publisher.Id
			};

			await _dbContext.AddAsync(courseClassroom);
			await _dbContext.SaveChangesAsync();
			await _subjectService.SaveAsync(subject);
			await _publisherService.SaveAsync(publisher);

			var publisherIds = new[]
				{course.Space!.PublisherId, course.PublisherId, classroom.PublisherId, publisher.Id};
			var actorIds = new[] {member.User!.ActorId, member.ActorId};
			var data = new {CourseClassroomId = courseClassroom.Id};
			var action = await _eventService.EmitAsync(publisherIds, actorIds, subject.Id, "COURSE_CLASSROOM_PUBLISHER",
				data);

			return new ActionResultModel<CourseClassroom>(courseClassroom, action);
		}
		
		
		
	}
}