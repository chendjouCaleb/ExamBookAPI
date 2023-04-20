using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Social;
using Social.EFCore;
using Vx;
using Vx.EFCore;

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
            
            services.AddDbContext<VxDbContext>(dbOptions =>
            {
                dbOptions.UseInMemoryDatabase("social");
            });
            
            services.AddSocial(_ =>
                { }).AddEntityFrameworkStores<SocialTestDbContext>();

            services.AddVx(_ => { })
                .AddEntityFrameworkStores<VxDbContext>()
                .AddNewtonSoftDataSerializer(_ => {});
            services.AddLogging();

            return services;
        }
    }
}