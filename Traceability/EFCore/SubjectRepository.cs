using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Traceability.Models;
using Traceability.Repositories;

namespace Traceability.EFCore
{
	public class SubjectEFRepository <TContext>: ISubjectRepository where TContext: TraceabilityDbContext
	{
		private readonly TContext _dbContext;


		public SubjectEFRepository(TContext dbContext)
		{
			_dbContext = dbContext;
		}

		public async Task<Subject?> GetByIdAsync(string id)
		{
			return await _dbContext.Subjects
				.Where(s => s.Id == id)
				.FirstOrDefaultAsync();
		}
        
		public Subject? GetById(string id)
		{
			return _dbContext.Subjects
				.FirstOrDefault(s => s.Id == id);
		}

		public async Task<ICollection<Subject>> GetByIdAsync(ICollection<string> id)
		{
			return await _dbContext.Subjects
				.Where(s => id.Contains(s.Id))
				.ToListAsync();
		}
        
		public ICollection<Subject> GetById(ICollection<string> id)
		{
			return _dbContext.Subjects
				.Where(s => id.Contains(s.Id))
				.ToList();
		}

		public async Task SaveAsync(Subject subject)
		{
			await _dbContext.AddAsync(subject);
			await _dbContext.SaveChangesAsync();
		}

		public async Task SaveAllAsync(ICollection<Subject> subject)
		{
			await _dbContext.AddRangeAsync(subject);
			await _dbContext.SaveChangesAsync();
		}

		public async Task DeleteAsync(Subject subject)
		{
			_dbContext.Remove(subject);
			await _dbContext.SaveChangesAsync();
		}

		public async Task UpdateAsync(Subject subject)
		{
			_dbContext.Update(subject);
			await _dbContext.SaveChangesAsync();
		}

		public void Delete(Subject subject)
		{
			_dbContext.Remove(subject);
			_dbContext.SaveChanges();
		}
	}
}