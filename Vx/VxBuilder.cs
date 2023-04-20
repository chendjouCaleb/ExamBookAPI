using System;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Vx.EFCore;
using Vx.JsonSerializer;
using Vx.Repositories;
using Vx.Serializers;

namespace Vx
{
    public class VxBuilder
    {
        private readonly IServiceCollection _services;

        public VxBuilder(IServiceCollection services)
        {
            _services = services;
        }
        
        public VxBuilder AddEntityFrameworkStores<TContext>() where TContext:VxDbContext
        {
            _services.AddTransient<IActorRepository, ActorEfRepository<TContext>>();
            _services.AddTransient<IEventRepository, EventEFRepository<TContext>>();
            _services.AddTransient<IPublisherRepository, PublisherEFRepository<TContext>>();
            _services.AddTransient<ISubscriptionRepository, SubscriptionEfRepository<TContext>>();
            return this;
        }

        public VxBuilder AddNewtonSoftDataSerializer(Action<JsonSerializerSettings> actionOptions)
        {
            JsonSerializerSettings settings = new ();
            actionOptions.Invoke(settings);

            _services.AddSingleton(settings);
            _services.AddSingleton<IDataSerializer, NewtonSoftDataSerializer>();
            return this;
        }
    }
}