using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Traceability;
using Traceability.EFCore;

namespace VxTest
{
    public static class ServiceProviderExtensions
    {
        public static IServiceCollection Setup(this IServiceCollection services)
        {
            services.AddDbContext<TraceabilityDbContext>(options =>
            {
                options.UseInMemoryDatabase("vx");
            });

            services.AddTraceability(_ => { })
                .AddEntityFrameworkStores<TraceabilityDbContext>()
                .AddNewtonSoftDataSerializer(_ => {});
                
            services.AddLogging();

            return services;
        }
    }
}