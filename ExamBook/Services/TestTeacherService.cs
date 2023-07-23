using System;
using System.Threading.Tasks;
using ExamBook.Entities;
using ExamBook.Models;
using ExamBook.Persistence;

namespace ExamBook.Services
{
	public class TestTeacherService
	{
		private readonly ApplicationDbContext _dbContext;


		public async Task<CourseTeacher> GetAsync(ulong courseTeacherId)
		{
			throw new NotImplementedException();
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