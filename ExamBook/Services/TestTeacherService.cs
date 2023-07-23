using System;
using System.Linq;
using System.Threading.Tasks;
using ExamBook.Entities;
using ExamBook.Exceptions;
using ExamBook.Models;
using ExamBook.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ExamBook.Services
{
	public class TestTeacherService
	{
		private readonly ApplicationDbContext _dbContext;


		public async Task<CourseTeacher> GetAsync(ulong courseTeacherId)
		{
			var courseTeacher = await _dbContext.TestTeachers
				.Include(tt => tt.Test)
				.Include(tt => tt.Member)
				.Where(tt => tt.Id == courseTeacherId)
				.FirstOrDefaultAsync();

			if (courseTeacher == null)
			{
				throw new ElementNotFoundException("CourseTeacherNotFoundById", courseTeacherId);
			}

			return courseTeacher;
		}

		public async Task<bool> ContainsAsync(Test test, Member member)
		{
			throw new NotImplementedException();
		}

		public async Task<ActionResultModel<CourseTeacher>> AddCourseTeacher(Test test, Member member)
		{
			throw new NotImplementedException();
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