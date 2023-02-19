using System;
using System.Threading.Tasks;
using ExamBook.Entities;
using ExamBook.Models;
using Microsoft.EntityFrameworkCore;

namespace ExamBook.Services
{
    public class ExaminationService
    {
        private readonly DbContext _dbContext;

        public ExaminationService(DbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<Examination> AddAsync(ExaminationAddModel model)
        {
            if (await ContainsAsync(model.Name))
            {
                throw new InvalidOperationException($"The name: {model.Name} is already used.");
            }

            if (model.StartAt < DateTime.Now)
            {
                
            }

            Examination examination = new ()
            {
                Name = model.Name,
                StartAt = model.StartAt
            };
            await _dbContext.AddAsync(examination);
            await _dbContext.SaveChangesAsync();
            return examination;
        }

        public async Task<Examination> FindAsync(string name)
        {
            var examination = await _dbContext.Set<Examination>()
                .FirstAsync(e => e.Name.Equals(name, StringComparison.InvariantCultureIgnoreCase));

            if (examination == null)
            {
                throw new InvalidOperationException($"Examination with name: {name} not found.");
            }

            return examination;
        }

        public async Task<bool> ContainsAsync(string name)
        {
            return await _dbContext.Set<Examination>()
                .AnyAsync(e => e.Name.Equals(name, StringComparison.InvariantCultureIgnoreCase));
        }

        public void ChangeInfo(Examination examination, ExaminationEditModel model)
        {
            
        }
        
        public void Delete(Examination examination) {}
    }
}