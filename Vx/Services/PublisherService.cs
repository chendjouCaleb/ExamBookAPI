using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using Vx.Models;
using Vx.Repositories;

namespace Vx.Services
{
    public class PublisherService
    {
        private readonly IPublisherRepository _publisherRepository;

        public PublisherService(IPublisherRepository publisherRepository)
        {
            _publisherRepository = publisherRepository;
        }

        
        /// <summary>
        /// Gets publisher by Id.
        /// </summary>
        /// <param name="publisherId">The id of the publisher to found.</param>
        /// <returns>The publisher found.</returns>
        /// <exception cref="InvalidOperationException">If the publisher not found.</exception>
        public async Task<Publisher> GetByIdAsync(string publisherId)
        {
            if (string.IsNullOrWhiteSpace(publisherId))
            {
                throw new ArgumentNullException(publisherId);
            }
            
            var publisher = await _publisherRepository.GetByIdAsync(publisherId);

            if (publisher == null)
            {
                throw new InvalidOperationException($"Publisher with id={publisherId} not found.");
            }

            return publisher;
        }
        
        public async Task<ImmutableList<Publisher>> GetByIdAsync(params string[] publisherIds)
        {
            var publishers = await _publisherRepository.GetByIdAsync(publisherIds);

            var publisherIdNotFounds = publisherIds.Where(publisherId => 
                    publishers.All(p => p.Id != publisherId))
                .ToList();

            if (publisherIdNotFounds.Count > 0)
            {
                throw new InvalidOperationException(
                    $"Publishers with id=[{string.Join(',', publisherIdNotFounds)}] not found.");
            }

            return publishers.ToImmutableList();
        }


        public async Task<IList<Publisher>> GetByIdAsync( ICollection<string> publisherIds)
        {
            var publishers = await _publisherRepository.GetByIdAsync(publisherIds);

            var publisherIdNotFounds = publisherIds.Where(publisherId => 
                publishers.All(p => p.Id != publisherId))
                .ToList();

            if (publisherIdNotFounds.Count > 0)
            {
                throw new InvalidOperationException(
                    $"Publishers with id=[{string.Join(',', publisherIdNotFounds)}] not found.");
            }

            return publishers.ToList();
        }

        /// <summary>
        /// Create a new publisher.
        /// </summary>
        /// <returns></returns>
        public async Task<Publisher> AddAsync()
        {
            Publisher publisher = new ();
            await _publisherRepository.SaveAsync(publisher);
            return publisher;
        }


        
        /// <summary>
        /// Delete a publisher by id.
        /// </summary>
        /// <param name="publisherId">The id of the publisher to delete.</param>
        public async Task Delete(string publisherId)
        {
            var publisher = await GetByIdAsync(publisherId);
            await DeleteAsync(publisher!);
        }


        /// <summary>
        /// Delete a publisher.
        /// </summary>
        /// <param name="publisher">Publisher to delete.</param>
        public async Task DeleteAsync(Publisher publisher)
        {
            await _publisherRepository.Delete(publisher);
        }
    }
}