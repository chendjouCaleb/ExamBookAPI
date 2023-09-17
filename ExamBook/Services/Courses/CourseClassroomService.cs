using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ExamBook.Entities;
using ExamBook.Exceptions;
using ExamBook.Helpers;
using ExamBook.Models;
using ExamBook.Models.Data;
using ExamBook.Persistence;
using ExamBook.Utils;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Traceability.Models;
using Traceability.Services;

namespace ExamBook.Services
{
	public class CourseClassroomService
	{
		private readonly ApplicationDbContext _dbContext;
		private readonly EventService _eventService;
		private readonly PublisherService _publisherService;
		private readonly SubjectService _subjectService;
		private readonly MemberService _memberService;
		private readonly SpecialityService _specialityService;
		private readonly ILogger<CourseClassroomService> _logger;

		public CourseClassroomService(PublisherService publisherService, 
			SubjectService subjectService, 
			ILogger<CourseClassroomService> logger, 
			EventService eventService, 
			ApplicationDbContext dbContext, MemberService memberService, SpecialityService specialityService)
		{
			_publisherService = publisherService;
			_subjectService = subjectService;
			_logger = logger;
			_eventService = eventService;
			_dbContext = dbContext;
			_memberService = memberService;
			_specialityService = specialityService;
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
		
		public async Task<bool> ContainsByCode(Classroom classroom, string code)
		{
			AssertHelper.NotNull(classroom, nameof(classroom));
            
			string normalizedCode = StringHelper.Normalize(code);
			return await _dbContext.Set<CourseClassroom>()
				.Where(c => c.NormalizedCode == normalizedCode)
				.Where(c => c.ClassroomId == classroom.Id)
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

			var members = await _memberService.ListAsync(model.MemberIds);
			var specialities = await _specialityService.ListAsync(model.SpecialityIds);
			
			if (await ContainsAsync(classroom, course))
			{
				throw new IllegalOperationException("CourseClassroomExists", course, classroom);
			}
			
			string normalizedCode = StringHelper.Normalize(model.Code);

			if (await ContainsByCode(classroom, model.Code))
			{
				throw new UsedValueException("CourseClassroomCodeUsed", classroom, model.Code);
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
			// var courseSpecialities = await _CreateCourseSpecialitiesAsync(course, specialities);
			// await _dbContext.AddRangeAsync(courseSpecialities);
			//
			// var courseTeachers = await _CreateCourseTeachersCourseAsync(course, members);
			// await _dbContext.AddRangeAsync(courseTeachers);
			
			
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
		
		
		public async Task<Event> ChangeCodeAsync(CourseClassroom courseClassroom, string code, Member adminMember)
		{
			AssertHelper.NotNull(courseClassroom, nameof(courseClassroom));
			AssertHelper.NotNull(courseClassroom.Classroom, nameof(courseClassroom.Classroom));
			AssertHelper.NotNull(adminMember, nameof(adminMember));

			var classroom = courseClassroom.Classroom;
			if (await ContainsByCode(classroom, code))
			{
				throw new UsedValueException("CourseCodeUsed", classroom, code);
			}

			var eventData = new ChangeValueData<string>(courseClassroom.Code, code);

			courseClassroom.Code = code;
			courseClassroom.NormalizedCode = StringHelper.Normalize(code);
			_dbContext.Update(courseClassroom);
			await _dbContext.SaveChangesAsync();
            
			var publisherIds = new List<string> {
				courseClassroom.PublisherId, 
				courseClassroom.Course.PublisherId,
				classroom.PublisherId,
				classroom.Space.PublisherId
			};
			var actorIds = new[] {adminMember.User!.ActorId, adminMember.ActorId};
			var name = "COURSE_CLASSROOM_CHANGE_CODE";
			return await _eventService.EmitAsync(publisherIds, actorIds, courseClassroom.SubjectId, name, eventData);
		}
		
		
		public async Task<Event> ChangeCoefficientAsync(CourseClassroom courseClassroom, uint coefficient, Member adminMember)
		{
			AssertHelper.NotNull(courseClassroom, nameof(courseClassroom));
			var eventData = new ChangeValueData<uint>(courseClassroom.Coefficient, coefficient);

			courseClassroom.Coefficient = coefficient;
			_dbContext.Update(courseClassroom);
			await _dbContext.SaveChangesAsync();

			var publisherIds = GetPublisherIds(courseClassroom);
			var actorIds = new[] {adminMember.User!.ActorId, adminMember.ActorId};
			var name = "COURSE_CLASSROOM_CHANGE_COEFFICIENT";
			return await _eventService.EmitAsync(publisherIds, actorIds, courseClassroom.SubjectId, name, eventData);
		}
		
		public async Task<Event> ChangeDescriptionAsync(CourseClassroom courseClassroom, string description, Member adminMember)
		{
			AssertHelper.NotNull(courseClassroom, nameof(courseClassroom));
			AssertHelper.NotNull(courseClassroom.Classroom, nameof(courseClassroom.Classroom));
			AssertHelper.NotNull(adminMember, nameof(adminMember));

			var eventData = new ChangeValueData<string>(courseClassroom.Description, description);

			courseClassroom.Description = description;
			_dbContext.Update(courseClassroom);
			await _dbContext.SaveChangesAsync();

			var publisherIds = GetPublisherIds(courseClassroom);
			var actorIds = new[] {adminMember.User!.ActorId, adminMember.ActorId};
			var name = "COURSE_CLASSROOM_CHANGE_DESCRIPTION";
			return await _eventService.EmitAsync(publisherIds, actorIds, courseClassroom.SubjectId, name, eventData);
		}


		public HashSet<string> GetPublisherIds(CourseClassroom courseClassroom)
		{
			return new HashSet<string> {
				courseClassroom.PublisherId, 
				courseClassroom.Course.PublisherId,
				courseClassroom.Classroom.PublisherId,
				courseClassroom.Classroom.Space.PublisherId
			};
		}
	}
}