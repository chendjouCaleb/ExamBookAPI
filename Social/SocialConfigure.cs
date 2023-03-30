using System;
using Microsoft.Extensions.DependencyInjection;
using Social.Services;

namespace Social
{
    public static class SocialConfigure
    {
        public static SocialBuilder AddSocial(this IServiceCollection services,
            Action<SocialOptions> optionAction)
        {
            var options = new SocialOptions();
            optionAction(options);

            var builder = new SocialBuilder(services);
            services.AddTransient<PostService>();
            services.AddTransient<AuthorService>();

            services.AddSingleton(options);

            return builder;
        }
    }
}