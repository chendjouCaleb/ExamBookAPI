using System;
using System.Linq;
using System.Threading.Tasks;
using ExamBook.Entities;
using ExamBook.Helpers;
using ExamBook.Models;
using ExamBook.Utils;
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

        public async Task<Examination> AddAsync(Space space, ExaminationAddModel model)
        {
            if (await ContainsAsync(space, model.Name))
            {
                throw new InvalidOperationException($"The name: {model.Name} is already used.");
            }

            if (model.StartAt < DateTime.Now)
            {
                
            }

            Examination examination = new ()
            {
                Space = space,
                Name = model.Name,
                StartAt = model.StartAt
            };
            await _dbContext.AddAsync(examination);
            await _dbContext.SaveChangesAsync();
            return examination;
        }


        public async Task<ExaminationSpeciality> AddSpeciality(Examination examination, 
            ExaminationSpecialityAddModel model)
        {
            Asserts.NotNull(examination, nameof(examination));
            Asserts.NotNull(model, nameof(model));

            if (await ContainsSpecialityAsync(examination, model.Name))
            {
                ExaminationHelper.ThrowDuplicateSpecialityNameError(examination, model.Name);
            }

            ExaminationSpeciality examinationSpeciality = new ()
            {
                Examination = examination,
                Name = model.Name,
                NormalizedName = model.Name.Normalize().ToUpper()
            };

            await _dbContext.AddAsync(examinationSpeciality);
            await _dbContext.SaveChangesAsync();
            return examinationSpeciality;
        }

        public async Task<Examination> FindAsync(string name)
        {
            Asserts.NotNullOrWhiteSpace(name, nameof(name));
            var examination = await _dbContext.Set<Examination>()
                .FirstAsync(e => e.Name.Equals(name, StringComparison.InvariantCultureIgnoreCase));

            if (examination == null)
            {
                throw new InvalidOperationException($"Examination with name: {name} not found.");
            }

            return examination;
        }
        
        
        public async Task<Examination> FindByIdAsync(ulong id)
        {

            var examination = await _dbContext.Set<Examination>().FindAsync(id);

            if (examination == null)
            {
                throw new InvalidOperationException($"Examination with id: {id} not found.");
            }

            return examination;
        }
        
        
        public async Task<Examination> FindSpecialityAsync(Examination examination, string name)
        {
            Asserts.NotNull(examination, nameof(examination));
            Asserts.NotNullOrWhiteSpace(name, nameof(name));
            var normalized = name.Normalize();
            var speciality = await _dbContext.Set<ExaminationSpeciality>()
                .FirstOrDefaultAsync(e => e.ExaminationId == examination.Id 
                                          && e.NormalizedName == normalized);


            if (speciality == null)
            {
                ExaminationHelper.ThrowSpecialityNotFound(examination, name);
            }

            return examination;
        }

        public async Task<bool> ContainsAsync(Space space, string name)
        {
            return await _dbContext.Set<Examination>()
                .AnyAsync(e => e.SpaceId == space.Id  &&
                               e.Name.Equals(name, StringComparison.InvariantCultureIgnoreCase));
        }
        
        public async Task<bool> ContainsSpecialityAsync(Examination examination, string name)
        {
            Asserts.NotNull(examination, nameof(examination));
            Asserts.NotNullOrWhiteSpace(name, nameof(name));
            
            return await _dbContext.Set<ExaminationSpeciality>()
                .AnyAsync(e => e.ExaminationId == examination.Id 
                               && e.Name.Equals(name, StringComparison.InvariantCultureIgnoreCase));
        }

        public async Task ChangeNameAsync(Examination examination, string name)
        {
            Asserts.NotNull(examination, nameof(examination));
            Asserts.NotNull(examination.Space, nameof(examination.Space));
            Asserts.NotNullOrWhiteSpace(name, nameof(name));

            if (await ContainsAsync(examination.Space, name))
            {
                ExaminationHelper.ThrowDuplicateNameError(examination.Space, name);
            }

            examination.Name = name;
            _dbContext.Update(examination);
            await _dbContext.SaveChangesAsync();
        }


        public async Task ChangeSpecialityName(ExaminationSpeciality examinationSpeciality,
            ExaminationSpecialityChangeNameModel model)
        {
            Asserts.NotNull(examinationSpeciality, nameof(examinationSpeciality));
            Asserts.NotNull(model, nameof(model));
            var examination = await FindByIdAsync(examinationSpeciality.ExaminationId);

            if (await ContainsSpecialityAsync(examination, model.Name))
            {
                ExaminationHelper.ThrowDuplicateSpecialityNameError(examination, model.Name);
            }

            examinationSpeciality.Name = model.Name;
            _dbContext.Update(examinationSpeciality);
            await _dbContext.SaveChangesAsync();
        }



        public async Task ChangeStartAtAsync(Examination examination, DateTime startAt)
        {
            Asserts.NotNull(examination, nameof(examination));

            examination.StartAt = startAt;
            _dbContext.Update(examination);
            await _dbContext.SaveChangesAsync();
        }
        
        public async Task DeleteSpecialityAsync(ExaminationSpeciality examinationSpeciality)
        {
            Asserts.NotNull(examinationSpeciality, nameof(examinationSpeciality));
            _dbContext.Remove(examinationSpeciality);
            await _dbContext.SaveChangesAsync();
        }

        public async Task DeleteAsync(Examination examination)
        {
            var specialities = _dbContext.Set<ExaminationSpeciality>()
                .Where(e => e.ExaminationId == examination.Id);
            
            _dbContext.RemoveRange(specialities);
            _dbContext.Remove(examination);
            await _dbContext.SaveChangesAsync();
        }
    }
}