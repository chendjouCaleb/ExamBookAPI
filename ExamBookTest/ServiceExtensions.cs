using System;
using System.IO;
using DriveIO;
using ExamBook.Entities;
using ExamBook.Identity;
using ExamBook.Identity.Models;
using ExamBook.Persistence;
using ExamBook.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Social;
using Vx;
using Vx.EFCore;

namespace ExamBookTest
{
    public static class ServiceExtensions
    {
        public static UserAddModel UserAddModel = new ()
        {
            FirstName = "first name",
            LastName = "last name",
            Sex = 'M',
            BirthDate = new DateTime(1995,1,1),
            UserName = "userName",
            Password = "Password09@"
        };
        
        public static UserAddModel UserAddModel2 = new ()
        {
            FirstName = "first name1",
            LastName = "last name2",
            Sex = 'M',
            BirthDate = new DateTime(1995,1,1),
            UserName = "userName1",
            Password = "Password09@"
        };
        
        public static IServiceProvider Setup(this IServiceCollection services)
        {
            services.AddDbContext<SocialTestDbContext>(options =>
            {
                options.UseInMemoryDatabase("vx");
                
            });
            
            services.AddDbContext<ApplicationIdentityDbContext>(options =>
            {
                options.EnableSensitiveDataLogging();
                options.EnableDetailedErrors();
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
                .AddNewtonSoftDataSerializer(options =>
                {
                    options.NullValueHandling = NullValueHandling.Ignore;
                });

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
            services.AddTransient<RoomService>();
            services.AddTransient<SpecialityService>();
            services.AddTransient<ClassroomService>();

            services.AddTransient<UserService>();
            services.AddIdentity<User, Role>()
                .AddEntityFrameworkStores<ApplicationIdentityDbContext>();
            services.AddLogging();
            
            IConfiguration configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.test.json")
                .Build();
            services.AddSingleton<IConfiguration>(configuration);

            var _provider = services.BuildServiceProvider();
            var identityDbContext = _provider.GetRequiredService<ApplicationIdentityDbContext>();
            var vxDbContext = _provider.GetRequiredService<VxTestDbContext>();
            var appDbContext = _provider.GetRequiredService<DbContext>();

            identityDbContext.Database.EnsureDeleted();
            appDbContext.Database.EnsureDeleted();
            vxDbContext.Database.EnsureDeleted();

            return _provider;
        }
    }
}