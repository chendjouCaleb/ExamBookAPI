using System;
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

        public async Task<Actor> AddAsync()
        {
            Actor actor = new();
            await _actorRepository.SaveAsync(actor);
            return actor;
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