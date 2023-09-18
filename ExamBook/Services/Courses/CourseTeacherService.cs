using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ExamBook.Entities;
using ExamBook.Exceptions;
using ExamBook.Models;
using ExamBook.Persistence;
using ExamBook.Utils;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Traceability.Models;
using Traceability.Services;

namespace ExamBook.Services
{
	public class CourseTeacherService
	{
		private readonly ApplicationDbContext _dbContext;
		private readonly EventService _eventService;
		private readonly SubjectService _subjectService;
		private readonly PublisherService _publisherService;
		private readonly ILogger<CourseTeacherService> _logger;

		public CourseTeacherService(ApplicationDbContext dbContext,
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
		

		public async Task<bool> ContainsAsync(CourseClassroom courseClassroom, ulong memberId)
		{
			var member = await _dbContext.Set<Member>().FindAsync(memberId);
			if (member == null)
			{
				throw new InvalidOperationException($"Member with id={memberId} not found.");
			}
			return await ContainsAsync(courseClassroom, member);
		}
		
		public async Task<List<CourseTeacher>> GetAllAsync(CourseClassroom courseClassroom,
			ICollection<Member> members)
		{
			var memberIds = members.Select(s => s.Id).ToHashSet();
			return await _dbContext.CourseTeachers
				.Where(cc => cc.CourseClassroomId == courseClassroom.Id && memberIds.Contains(cc.MemberId ?? 0))
				.ToListAsync();
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
			
			var duplicate = await GetAllAsync(courseClassroom, members);
			if (members.Any())
			{
				throw new DuplicateValueException("CourseTeacherExists", courseClassroom, duplicate);
			}

			var courseTeachers = _CreateCourseTeachers(courseClassroom, members);
			var subjects = courseTeachers.Select(ct => ct.Subject).ToList();
			var publishers = courseTeachers.Select(ct => ct.Publisher!).ToList();
			await _dbContext.AddRangeAsync(courseTeachers);
			await _dbContext.SaveChangesAsync();

			await _subjectService.SaveAllAsync(subjects);
			await _publisherService.SaveAllAsync(publishers);

			var otherPublisherIds = courseClassroom.GetPublisherIds()
				.Concat(members.Select(s => s.PublisherId)).ToHashSet();
			var otherPublishers = await _eventService.GetPublishers(otherPublisherIds);


			publishers = publishers.Concat(otherPublishers).ToList();
			var actors = await _eventService.GetActors(adminMember.GetActorIds());
			var data = new {CourseTeacherIds = courseTeachers.Select(ct => ct.Id).ToList()};
			
			var action = await _eventService.EmitAsync(publishers, subjects, actors,"COURSE_TEACHERS_ADD", data);
			
			_logger.LogInformation("New course teacher: {}", data);
			return new ActionResultModel<ICollection<CourseTeacher>>(courseTeachers, action);
		}


		public CourseTeacher _CreateCourseTeacher(CourseClassroom courseClassroom, Member member)
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

		public List<CourseTeacher> _CreateCourseTeachers(CourseClassroom courseClassroom, IEnumerable<Member> members)
		{
			AssertHelper.NotNull(courseClassroom, nameof(courseClassroom));
			AssertHelper.NotNull(members, nameof(members));

			return members.Select(member => _CreateCourseTeacher(courseClassroom, member)).ToList();
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
			AssertHelper.NotNull(courseTeacher.CourseClassroom.Course, nameof(courseTeacher.CourseClassroom.Course));
			AssertHelper.NotNull(courseTeacher.CourseClassroom.Course.Space,
				nameof(courseTeacher.CourseClassroom.Course.Space));
			AssertHelper.NotNull(courseTeacher.Member, nameof(courseTeacher.Member));
		}

		public static HashSet<string> GetPublisherIds(CourseTeacher courseTeacher)
		{
			return new HashSet<string>
			{
				courseTeacher.PublisherId,
				courseTeacher.CourseClassroom.PublisherId,
				courseTeacher.CourseClassroom.Course.PublisherId,
				courseTeacher.CourseClassroom.Course.Space.PublisherId,
				courseTeacher.Member!.PublisherId
			};
		}
	}
}