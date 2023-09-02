using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Traceability.Models;
using Traceability.Repositories;

namespace Traceability.Services
{
    public class ActorService
    {
        private readonly IActorRepository _actorRepository;

        public ActorService(IActorRepository actorRepository)
        {
            _actorRepository = actorRepository;
        }

        public async Task<Actor> GetByIdAsync(string id)
        {
            var actor = await _actorRepository.GetByIdAsync(id);

            if (actor == null)
            {
                throw new InvalidOperationException($"Actor with id={id} not found.");
            }

            return actor;
        }
        
        public async Task<IList<Actor>> GetByIdAsync( ICollection<string> actorIds)
        {
            var actors = await _actorRepository.GetByIdAsync(actorIds);

            var actorIdNotFounds = actorIds.Where(actorId => 
                    actors.All(p => p.Id != actorId))
                .ToList();

            if (actorIdNotFounds.Count > 0)
            {
                throw new InvalidOperationException(
                    $"Actors with id=[{string.Join(',', actorIdNotFounds)}] not found.");
            }

            return actors.ToList();
        }

       
        public Actor Create(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentNullException(nameof(name));
            }
            return new Actor {Name = name};
        }
        
        public async Task SaveAllAsync(ICollection<Actor> actors)
        {
            await _actorRepository.SaveAllAsync(actors);
        }
        
        public async Task SaveAsync(Actor actor)
        {
            await _actorRepository.SaveAsync(actor);
        }

        public async Task DeleteAsync(string actorId)
        {
            var actor = await GetByIdAsync(actorId);
            await DeleteAsync(actor);
        }

        public async Task DeleteAsync(Actor actor)
        {
            await _actorRepository.DeleteAsync(actor);
        }
    }
}