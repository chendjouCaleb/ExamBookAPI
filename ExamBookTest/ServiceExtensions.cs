using System.IO;
using DriveIO;
using ExamBook.Identity;
using ExamBook.Identity.Models;
using ExamBook.Persistence;
using ExamBook.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Social;
using Vx;
using Vx.EFCore;

namespace ExamBookTest
{
    public static class ServiceExtensions
    {
        public static IServiceCollection Setup(this IServiceCollection services)
        {
            services.AddDbContext<SocialTestDbContext>(options =>
            {
                options.UseInMemoryDatabase("vx");
                
            });
            
            services.AddDbContext<ApplicationIdentityDbContext>(options =>
            {
                options.UseInMemoryDatabase("identity");
                
            });
            
            services.AddDbContext<VxTestDbContext>(dbOptions =>
            {
                dbOptions.UseInMemoryDatabase("social");
            });

            services.AddDbContext<DriveTestDbContext>(options =>
            {
                options.UseInMemoryDatabase("drive");
            });
            
            services.AddDbContext<DbContext, ApplicationDbContext>(options =>
            {
                options.UseInMemoryDatabase("exambook");
            });
            
            
            
            services.AddSocial(_ =>
                { }).AddEntityFrameworkStores<SocialTestDbContext>();

            services.AddVx(_ => { })
                .AddEntityFrameworkStores<VxTestDbContext>()
                .AddNewtonSoftDataSerializer(_ => {});

            services.AddDrive(options =>
                {
                    
                })
                .AddEntityFrameworkStores<DriveTestDbContext>()
                .AddLocalFileStores(storeOptions =>
                {
                    storeOptions.DirectoryPath = "drive";
                    storeOptions.CreateDirPath = true;
                });

            services.AddTransient<SpaceService>();
            services.AddTransient<MemberService>();

            services.AddTransient<UserService>();
            services.AddIdentity<User, Role>()
                .AddEntityFrameworkStores<ApplicationIdentityDbContext>();
            services.AddLogging();
            
            IConfiguration configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.test.json")
                .Build();
            services.AddSingleton<IConfiguration>(configuration);

            return services;
        }
    }
}