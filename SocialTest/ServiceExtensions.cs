using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Social;
using Social.EFCore;
using Traceability;
using Traceability.EFCore;

namespace SocialTest
{
    public static class ServiceExtensions
    {
        public static IServiceCollection Setup(this IServiceCollection services)
        {
            services.AddDbContext<SocialTestDbContext>(options =>
            {
                options.UseInMemoryDatabase("vx");
            });
            
            services.AddDbContext<TraceabilityDbContext>(dbOptions =>
            {
                dbOptions.UseInMemoryDatabase("social");
            });
            
            services.AddSocial(_ =>
                { }).AddEntityFrameworkStores<SocialTestDbContext>();

            services.AddTraceability(_ => { })
                .AddEntityFrameworkStores<TraceabilityDbContext>()
                .AddNewtonSoftDataSerializer(_ => {});
            services.AddLogging();

            return services;
        }
    }
}