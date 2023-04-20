using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Social.Entities;
using Social.Helpers;
using Social.Repositories;

namespace Social.Services
{
    public class ReactionService
    {
        private readonly IReactionRepository _reactionRepository;


        public ReactionService(IReactionRepository reactionRepository)
        {
            _reactionRepository = reactionRepository;
        }

        public async Task<Reaction> GetByIdAsync(long id)
        {
            var reaction = await _reactionRepository.GetByIdAsync(id);

            if (reaction == null)
            {
                throw new InvalidOperationException($"The reaction with id={id} not found.");
            }

            return reaction;
        }


        public async Task<IEnumerable<Reaction>> GetPostReactions(Post post, string type)
        {
            return await _reactionRepository.GetPostReactions(post, type);
        }

        public async Task<Reaction> ReactAsync(Post post, Author author, string type)
        {
            var reaction = await _reactionRepository.GetByPostAuthor(post, author);
            
            if (reaction == null)
            {
                reaction = await AddAsync(post, author, type);
            }
            else
            {
               await UpdateReactionAsync(post, author, type);
            }

            return reaction;
        }


        public async Task<Reaction> AddAsync(Post post, Author author, string type)
        {
            if (await _reactionRepository.ExistsByPostAuthor(post, author))
            {
                throw new InvalidOperationException($"The author[id={author.Id}] has react to post[id={post.Id}]");
            }

            Reaction reaction = new()
            {
                Post = post,
                Author = author,
                Type = StringHelper.Normalize(type)
            };

            await _reactionRepository.SaveAsync(reaction);
            return reaction;
        }


        public async Task UpdateReactionAsync(Post post, Author author, string type)
        {
            var reaction = await _reactionRepository.GetByPostAuthor(post, author);
            
            if (reaction == null)
            {
                throw new InvalidOperationException($"The author[id={author.Id}] has not react to post[id={post.Id}].");
            }

            reaction.Type = StringHelper.Normalize(type);
            await _reactionRepository.UpdateAsync(reaction);
        }

        
        public async Task DeleteAsync(long reactionId)
        {
            var reaction = await GetByIdAsync(reactionId);
            await DeleteAsync(reaction);
        }

        public async Task DeleteAsync(Reaction reaction)
        {
            await _reactionRepository.DeleteAsync(reaction);
        }
        
    }
}