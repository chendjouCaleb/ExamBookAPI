using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using ExamBook.Entities;
using ExamBook.Exceptions;
using ExamBook.Models;
using ExamBook.Utils;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Traceability.Models;
using Traceability.Services;

namespace ExamBook.Services
{
	public class CourseTeacherService
	{
		private readonly DbContext _dbContext;
		private readonly EventService _eventService;
		private readonly SubjectService _subjectService;
		private readonly PublisherService _publisherService;
		private readonly ILogger<CourseTeacherService> _logger;

		public CourseTeacherService(DbContext dbContext,
			EventService eventService,
			ILogger<CourseTeacherService> logger, SubjectService subjectService, PublisherService publisherService)
		{
			_dbContext = dbContext;
			_eventService = eventService;
			_logger = logger;
			_subjectService = subjectService;
			_publisherService = publisherService;
		}

		public async Task<CourseTeacher> GetAsync(ulong id)
		{
			var courseTeacher = await _dbContext.Set<CourseTeacher>()
				.Include(ct => ct.Member)
				.Where(ct => ct.Id == id)
				.FirstOrDefaultAsync();

			if (courseTeacher == null)
			{
				throw new ElementNotFoundException("CourseTeacherNotFoundById");
			}

			return courseTeacher;
		}


		public async Task<bool> ContainsAsync(CourseClassroom courseClassroom, Member member)
		{
			return await _dbContext.Set<CourseTeacher>()
				.Where(ct => ct.CourseClassroomId == courseClassroom.Id && ct.MemberId == member.Id)
				.Where(ct => ct.DeletedAt == null)
				.AnyAsync();
		}


		public async Task<ActionResultModel<CourseTeacher>> AddAsync(CourseClassroom courseClassroom,
			Member member, Member adminMember)
		{
			AssertHelper.NotNull(courseClassroom, nameof(courseClassroom));
			AssertHelper.NotNull(member, nameof(member));
			AssertHelper.NotNull(adminMember, nameof(adminMember));
			AssertHelper.NotNull(member, nameof(member));

			if (await ContainsAsync(courseClassroom, member))
			{
				throw new IllegalOperationException("CourseTeacherAlreadyExists", courseClassroom, member);
			}


			CourseTeacher courseTeacher = await _CreateCourseTeacherAsync(courseClassroom, member);
			await _dbContext.AddAsync(courseTeacher);
			await _dbContext.SaveChangesAsync();

			await _publisherService.SaveAsync(courseTeacher.Publisher!);
			await _subjectService.SaveAsync(courseTeacher.Subject);

			var publisherIds = GetPublisherIds(courseTeacher);
			var actorIds = new[] {adminMember.ActorId, adminMember.User!.ActorId};
			var data = new {CourseTeacher = courseTeacher.Id};
			var @event = await _eventService.EmitAsync(publisherIds, actorIds, courseTeacher.SubjectId,
				"COURSE_TEACHER_ADD", data);
			_logger.LogInformation("New course Teacher: {}", data);
			return new ActionResultModel<CourseTeacher>(courseTeacher, @event);
		}


		public async Task<ActionResultModel<ICollection<CourseTeacher>>> AddCourseTeachersAsync(
			CourseClassroom courseClassroom,
			ICollection<Member> members, 
			Member adminMember)
		{
			AssertHelper.NotNull(courseClassroom, nameof(courseClassroom));
			AssertHelper.NotNull(courseClassroom.Course.Space, nameof(courseClassroom.Course.Space));
			AssertHelper.NotNull(members, nameof(members));
			AssertHelper.NotNull(adminMember, nameof(adminMember));

			var courseTeachers = await _CreateCourseTeachersAsync(courseClassroom, members);
			var subjects = courseTeachers.Select(ct => ct.Subject).ToList();
			var publishers = courseTeachers.Select(ct => ct.Publisher!).ToList();
			await _dbContext.AddRangeAsync(courseTeachers);
			await _dbContext.SaveChangesAsync();

			await _subjectService.SaveAllAsync(subjects);
			await _publisherService.SaveAllAsync(publishers);


			var publisherIds = ImmutableList
				.Create(courseClassroom.Course.Space!.PublisherId,
					courseClassroom.Course.PublisherId,
					courseClassroom.PublisherId)
				.AddRange(members.Select(s => s.PublisherId))
				.AddRange(publishers.Select(p => p.Id));
			var subjectIds = subjects.Select(s => s.Id).ToList();
			var actorIds = new[] { adminMember.ActorId, adminMember.User.ActorId};
			var @event =
				await _eventService.EmitAsync(publisherIds, subjectIds, actorIds, "COURSE_TEACHERS_ADD", courseTeachers);

			return new ActionResultModel<ICollection<CourseTeacher>>(courseTeachers, @event);
		}


		public async Task<CourseTeacher> _CreateCourseTeacherAsync(CourseClassroom courseClassroom, Member member)
		{
			AssertHelper.NotNull(courseClassroom, nameof(courseClassroom));
			AssertHelper.NotNull(member, nameof(member));
			AssertHelper.NotNull(courseClassroom.Course.Space, nameof(courseClassroom.Course.Space));

			if (courseClassroom.Course.SpaceId != member.SpaceId)
			{
				throw new IncompatibleEntityException(courseClassroom, member);
			}

			var subject = _subjectService.Create("COURSE_TEACHER_SUBJECT");
			var publisher = _publisherService.Create("COURSE_TEACHER_PUBLISHER");

			CourseTeacher courseTeacher = new()
			{
				CourseClassroom = courseClassroom,
				Member = member,
				Subject = subject,
				SubjectId = subject.Id,
				Publisher = publisher,
				PublisherId = publisher.Id
			};
			return courseTeacher;
		}

		public async Task<List<CourseTeacher>> _CreateCourseTeachersAsync(CourseClassroom courseClassroom,
			IEnumerable<Member> members)
		{
			AssertHelper.NotNull(courseClassroom, nameof(courseClassroom));
			AssertHelper.NotNull(members, nameof(members));

			var courseSpecialities = new List<CourseTeacher>();
			foreach (var member in members)
			{
				var courseMember = await _CreateCourseTeacherAsync(courseClassroom, member);
				courseSpecialities.Add(courseMember);
			}

			return courseSpecialities;
		}
		
		
		public async Task<bool> CourseTeacherExistsAsync(Course course, Member member)
		{
			AssertHelper.NotNull(course, nameof(course));
			AssertHelper.NotNull(member, nameof(member));

			return await _dbContext.Set<CourseTeacher>()
				.Where(ct => ct.CourseId == course.Id)
				.Where(ct => ct.MemberId == member.Id)
				.Where(ct => ct.DeletedAt == null)
				.AnyAsync();
		}

		public async Task<bool> CourseTeacherExists(Course course, ulong memberId)
		{
			var member = await _dbContext.Set<Member>().FindAsync(memberId);
			if (member == null)
			{
				throw new InvalidOperationException($"Member with id={memberId} not found.");
			}
			return await CourseTeacherExistsAsync(course, member);
		}


		public async Task<List<CourseTeacher>> _CreateCourseTeachersCourseAsync(Course course, List<Member> members)
		{
			var courseTeachers = new List<CourseTeacher>();
			foreach (var member in members)
			{
				if (!await CourseTeacherExistsAsync(course, member))
				{
					var courseTeacher = _CreateCourseTeacherAsync(course, member);
					courseTeachers.Add(courseTeacher);
				}
			}

			return courseTeachers;
		}

		public async Task<Event> SetAsPrincipalAsync(CourseTeacher courseTeacher, Member member)
		{
			AssertNotNull(courseTeacher);
			AssertHelper.NotNull(member, nameof(member));

			if (!courseTeacher.IsPrincipal)
			{
				throw new IllegalStateException("CourseTeacherIsAlreadyPrincipal");
			}

			courseTeacher.IsPrincipal = true;
			_dbContext.Update(courseTeacher);
			await _dbContext.SaveChangesAsync();

			var publisherIds = GetPublisherIds(courseTeacher);
			var actorIds = new[] {member.ActorId, member.User!.ActorId};
			return await _eventService.EmitAsync(publisherIds, actorIds, courseTeacher.SubjectId,
				"COURSE_TEACHER_SET_PRINCIPAL", new { });
		}


		public async Task<Event> UnSetAsPrincipalAsync(CourseTeacher courseTeacher, Member member)
		{
			AssertNotNull(courseTeacher);
			AssertHelper.NotNull(member, nameof(member));

			if (courseTeacher.IsPrincipal)
			{
				throw new IllegalStateException("CourseTeacherIsPrincipal");
			}

			courseTeacher.IsPrincipal = false;
			_dbContext.Update(courseTeacher);
			await _dbContext.SaveChangesAsync();

			var publisherIds = GetPublisherIds(courseTeacher);
			var actorIds = new[] {member.ActorId, member.User!.ActorId};
			return await _eventService.EmitAsync(publisherIds, actorIds, courseTeacher.SubjectId,
				"COURSE_TEACHER_UNSET_PRINCIPAL", new { });
		}


		public async Task<Event> DeleteAsync(CourseTeacher courseTeacher, Member member)
		{
			AssertNotNull(courseTeacher);
			AssertHelper.NotNull(member, nameof(member));

			courseTeacher.DeletedAt = DateTime.UtcNow;
			_dbContext.Update(courseTeacher);
			await _dbContext.SaveChangesAsync();


			var publisherIds = GetPublisherIds(courseTeacher);
			var actorIds = new[] {member.ActorId, member.User!.ActorId};
			var data = new {CourseTeacherId = courseTeacher.Id};
			return await _eventService.EmitAsync(publisherIds, actorIds, courseTeacher.SubjectId,
				"COURSE_TEACHER_DELETE", data);
		}

		public static void AssertNotNull(CourseTeacher courseTeacher)
		{
			AssertHelper.NotNull(courseTeacher, nameof(courseTeacher));
			AssertHelper.NotNull(courseTeacher.Member, nameof(courseTeacher.Member));
			AssertHelper.NotNull(courseTeacher.CourseClassroom, nameof(courseTeacher.CourseClassroom));
			AssertHelper.NotNull(courseTeacher.CourseClassroom!.Course, nameof(courseTeacher.CourseClassroom.Course));
			AssertHelper.NotNull(courseTeacher.CourseClassroom.Course.Space,
				nameof(courseTeacher.CourseClassroom.Course.Space));
			AssertHelper.NotNull(courseTeacher.Member, nameof(courseTeacher.Member));
		}

		public static HashSet<string> GetPublisherIds(CourseTeacher courseTeacher)
		{
			return new HashSet<string>
			{
				courseTeacher.PublisherId,
				courseTeacher.CourseClassroom!.PublisherId,
				courseTeacher.CourseClassroom.Course.PublisherId,
				courseTeacher.CourseClassroom.Course.Space!.PublisherId,
				courseTeacher.Member!.PublisherId
			};
		}
	}
}