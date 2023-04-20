using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Vx;
using Vx.EFCore;

namespace VxTest
{
    public static class ServiceProviderExtensions
    {
        public static IServiceCollection Setup(this IServiceCollection services)
        {
            services.AddDbContext<VxDbContext>(options =>
            {
                options.UseInMemoryDatabase("vx");
            });

            services.AddVx(_ => { })
                .AddEntityFrameworkStores<VxDbContext>()
                .AddNewtonSoftDataSerializer(_ => {});
                
            services.AddLogging();

            return services;
        }
    }
}