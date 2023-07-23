using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ExamBook.Entities;
using ExamBook.Exceptions;
using ExamBook.Identity.Entities;
using ExamBook.Models;
using ExamBook.Persistence;
using ExamBook.Utils;
using Microsoft.EntityFrameworkCore;
using Traceability.Services;

namespace ExamBook.Services
{
	public class TestTeacherService
	{
		private readonly ApplicationDbContext _dbContext;
		private readonly SubjectService _subjectService;
		private readonly PublisherService _publisherService;
		private readonly EventService _eventService;


		public async Task<TestTeacher> GetAsync(ulong courseTeacherId)
		{
			var testTeacher = await _dbContext.TestTeachers
				.Include(tt => tt.Test)
				.Include(tt => tt.Member)
				.Where(tt => tt.Id == courseTeacherId)
				.FirstOrDefaultAsync();

			if (testTeacher == null)
			{
				throw new ElementNotFoundException("TestTeacherNotFoundById", courseTeacherId);
			}

			return testTeacher;
		}

		public async Task<bool> ContainsAsync(Test test, Member member)
		{
			AssertHelper.NotNull(test, nameof(test));
			AssertHelper.NotNull(member, nameof(member));

			return await _dbContext.TestTeachers
				.AnyAsync(tt => tt.TestId == test.Id && tt.MemberId == member.Id);
		}

		public async Task<ActionResultModel<TestTeacher>> AddCourseTeacher(Test test, Member member, User user)
		{
			AssertHelper.NotNull(test, nameof(test));
			AssertHelper.NotNull(member, nameof(member));
			AssertHelper.NotNull(user, nameof(user));
			AssertHelper.IsTrue(test.SpaceId == member.SpaceId);

			if (await ContainsAsync(test, member))
			{
				throw new DuplicateValueException("DuplicateTestTeacher", test, member);
			}

			var publisher = _publisherService.Create();
			var subject = _subjectService.Create();
			TestTeacher testTeacher = new()
			{
				Test = test,
				Member = member,
				PublisherId = publisher.Id,
				Publisher = publisher,
				Subject = subject,
				SubjectId = subject.Id
			};

			await _dbContext.AddAsync(testTeacher);
			await _dbContext.SaveChangesAsync();
			await _publisherService.SaveAsync(publisher);
			await _subjectService.SaveAsync(subject);

			var publisherId = new List<string>() { test.Space.PublisherId, publisher.Id };

			if (test.ExaminationId != null)
			{
				AssertHelper.NotNull(test.Examination, nameof(test.Examination));
				publisherId.Add(test.Examination!.PublisherId);
			}

			var action = await _eventService.EmitAsync(publisherId, user.ActorId, "TEST_TEACHER_ADD", testTeacher);
			return new ActionResultModel<TestTeacher>(testTeacher, action);
		}

		public async Task<CourseTeacher> CreateAsync(Test test, Member member)
		{
			throw new NotImplementedException();
		}

		public async Task DeleteAsync(TestTeacher testTeacher)
		{
			throw new NotImplementedException();	
		}
	}
}