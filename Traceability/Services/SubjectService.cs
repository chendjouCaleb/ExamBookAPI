using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using Traceability.Models;
using Traceability.Repositories;

namespace Traceability.Services
{
    public class SubjectService
    {
        private readonly ISubjectRepository _subjectRepository;

        public SubjectService(ISubjectRepository subjectRepository)
        {
            _subjectRepository = subjectRepository;
        }

        
        /// <summary>
        /// Gets subject by Id.
        /// </summary>
        /// <param name="subjectId">The id of the subject to found.</param>
        /// <returns>The subject found.</returns>
        /// <exception cref="InvalidOperationException">If the subject not found.</exception>
        public async Task<Subject> GetByIdAsync(string subjectId)
        {
            if (string.IsNullOrWhiteSpace(subjectId))
            {
                throw new ArgumentNullException(subjectId);
            }
            
            var subject = await _subjectRepository.GetByIdAsync(subjectId);

            if (subject == null)
            {
                throw new InvalidOperationException($"Subject with id={subjectId} not found.");
            }

            return subject;
        }
        
        
        /// <summary>
        /// Gets subject by Id.
        /// </summary>
        /// <param name="subjectId">The id of the subject to found.</param>
        /// <returns>The subject found.</returns>
        /// <exception cref="InvalidOperationException">If the subject not found.</exception>
        public Subject GetById(string subjectId)
        {
            if (string.IsNullOrWhiteSpace(subjectId))
            {
                throw new ArgumentNullException(subjectId);
            }
            
            var subject = _subjectRepository.GetById(subjectId);

            if (subject == null)
            {
                throw new InvalidOperationException($"Subject with id={subjectId} not found.");
            }

            return subject;
        }
        
        public async Task<ImmutableList<Subject>> GetByIdAsync(params string[] subjectIds)
        {
            var subjects = await _subjectRepository.GetByIdAsync(subjectIds);

            var subjectIdNotFounds = subjectIds.Where(subjectId => 
                    subjects.All(p => p.Id != subjectId))
                .ToList();

            if (subjectIdNotFounds.Count > 0)
            {
                throw new InvalidOperationException(
                    $"Subjects with id=[{string.Join(',', subjectIdNotFounds)}] not found.");
            }

            return subjects.ToImmutableList();
        }


        public async Task<IList<Subject>> GetByIdAsync( ICollection<string> subjectIds)
        {
            var subjects = await _subjectRepository.GetByIdAsync(subjectIds);

            var subjectIdNotFounds = subjectIds.Where(subjectId => 
                subjects.All(p => p.Id != subjectId))
                .ToList();

            if (subjectIdNotFounds.Count > 0)
            {
                throw new InvalidOperationException(
                    $"Subjects with id=[{string.Join(',', subjectIdNotFounds)}] not found.");
            }

            return subjects.ToList();
        }

        /// <summary>
        /// Create a new subject.
        /// </summary>
        /// <returns></returns>
        public async Task<Subject> AddAsync()
        {
            Subject subject = new ();
            await _subjectRepository.SaveAsync(subject);
            return subject;
        }

        public async Task SaveAll(ICollection<Subject> subjects)
        {
            await _subjectRepository.SaveAllAsync(subjects);
        }

        public async Task<Subject> CreateAsync()
        {
            return new Subject();
        }



        /// <summary>
        /// Delete a subject by id.
        /// </summary>
        /// <param name="subjectId">The id of the subject to delete.</param>
        public async Task Delete(string subjectId)
        {
            var subject = await GetByIdAsync(subjectId);
            await DeleteAsync(subject!);
        }


        /// <summary>
        /// Delete a subject.
        /// </summary>
        /// <param name="subject">Subject to delete.</param>
        public async Task DeleteAsync(Subject subject)
        {
            await _subjectRepository.DeleteAsync(subject);
        }
    }
}