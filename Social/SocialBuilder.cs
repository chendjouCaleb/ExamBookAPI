using Microsoft.Extensions.DependencyInjection;
using Social.EFCore;
using Social.Repositories;

namespace Social
{
    public class SocialBuilder
    {
        private readonly IServiceCollection _services;

        public SocialBuilder(IServiceCollection services)
        {
            _services = services;
        }
        
        public SocialBuilder AddEntityFrameworkStores<TContext>() where TContext:SocialDbContext
        {
            _services.AddTransient<IPostRepository, PostEFRepository<TContext>>();
            _services.AddTransient<IAuthorRepository, AuthorEFRepository<TContext>>();
            _services.AddTransient<IReactionRepository, ReactionEFRepository<TContext>>();
            _services.AddTransient<IRepostRepository, RepostEFRepository<TContext>>();
            return this;
        }
    }
}