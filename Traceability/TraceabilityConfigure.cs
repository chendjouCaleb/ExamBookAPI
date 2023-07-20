using System;
using Microsoft.Extensions.DependencyInjection;
using Traceability.Asserts;
using Traceability.Services;

namespace Traceability
{
    public static class TraceabilityConfigure
    {
        public static TraceabilityBuilder AddTraceability(this IServiceCollection services,
            Action<TraceabilityOptions> optionAction)
        {
            var options = new TraceabilityOptions();
            optionAction(options);

            var builder = new TraceabilityBuilder(services);
            services.AddTransient<ActorService>();
            services.AddTransient<EventService>();
            services.AddTransient<PublisherService>();
            services.AddTransient<SubscriptionService>();
            services.AddTransient<SubjectService>();
            services.AddSingleton<EventAssertionsBuilder>();

            services.AddSingleton(options);

            return builder;
        }
    }
}