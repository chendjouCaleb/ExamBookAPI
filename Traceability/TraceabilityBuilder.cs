using System;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Traceability.EFCore;
using Traceability.JsonSerializer;
using Traceability.Repositories;
using Traceability.Serializers;

namespace Traceability
{
    public class TraceabilityBuilder
    {
        private readonly IServiceCollection _services;

        public TraceabilityBuilder(IServiceCollection services)
        {
            _services = services;
        }
        
        public TraceabilityBuilder AddEntityFrameworkStores<TContext>() where TContext:TraceabilityDbContext
        {
            _services.AddTransient<IActorRepository, ActorEfRepository<TContext>>();
            _services.AddTransient<IEventRepository, EventEFRepository<TContext>>();
            _services.AddTransient<IPublisherRepository, PublisherEFRepository<TContext>>();
            _services.AddTransient<ISubscriptionRepository, SubscriptionEfRepository<TContext>>();
            _services.AddTransient<ISubjectRepository, SubjectEFRepository<TContext>>();
            return this;
        }

        public TraceabilityBuilder AddNewtonSoftDataSerializer(Action<JsonSerializerSettings> actionOptions)
        {
            JsonSerializerSettings settings = new ();
            actionOptions.Invoke(settings);

            _services.AddSingleton(settings);
            _services.AddSingleton<IDataSerializer, NewtonSoftDataSerializer>();
            return this;
        }
    }
}