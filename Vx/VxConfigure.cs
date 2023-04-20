using System;
using Microsoft.Extensions.DependencyInjection;

using Vx.Services;

namespace Vx
{
    public static class VxConfigure
    {
        public static VxBuilder AddVx(this IServiceCollection services,
            Action<VxOptions> optionAction)
        {
            var options = new VxOptions();
            optionAction(options);

            var builder = new VxBuilder(services);
            services.AddTransient<ActorService>();
            services.AddTransient<EventService>();
            services.AddTransient<PublisherService>();
            services.AddTransient<SubscriptionService>();

            services.AddSingleton(options);

            return builder;
        }
    }
}