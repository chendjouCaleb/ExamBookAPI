using System;
using System.IO;
using DriveIO;
using ExamBook.Entities;
using ExamBook.Identity;
using ExamBook.Identity.Entities;
using ExamBook.Identity.Models;
using ExamBook.Identity.Services;
using ExamBook.Persistence;
using ExamBook.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Social;
using Traceability;
using Traceability.EFCore;

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
            Password = "Password09@",
            Email = "user@gmail.com"
        };
        
        public static UserAddModel UserAddModel1 = new ()
        {
            FirstName = "first name1",
            LastName = "last name2",
            Sex = 'M',
            BirthDate = new DateTime(1995,1,1),
            UserName = "userName12",
            Password = "Password09@",
            Email = "user1@gmail.com"
        };
        
        public static UserAddModel UserAddModel2 = new ()
        {
            FirstName = "first name1",
            LastName = "last name2",
            Sex = 'M',
            BirthDate = new DateTime(1995,1,1),
            UserName = "userName2",
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
            
            services.AddDbContext<TraceabilityTestDbContext>(dbOptions =>
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

            services.AddTraceability(_ => { })
                .AddEntityFrameworkStores<TraceabilityTestDbContext>()
                .AddNewtonSoftDataSerializer(options =>
                {
                    options.NullValueHandling = NullValueHandling.Ignore;
                    options.ReferenceLoopHandling = ReferenceLoopHandling.Ignore;
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

            services.AddTransient<SpaceService>()
                .AddTransient<MemberService>()
                .AddTransient<RoomService>()
                .AddTransient<SpecialityService>()

                .AddTransient<StudentService>()
                .AddTransient<StudentSpecialityService>()
                .AddTransient<CourseService>()
                .AddTransient<CourseTeacherService>()
                .AddTransient<CourseHourService>()
                .AddTransient<CourseSessionService>()
                
                .AddTransient<ExaminationService>()
                .AddTransient<ExaminationSpecialityService>()
                .AddTransient<ParticipantService>()
                
                .AddTransient<TestService>()
                .AddTransient<TestSpecialityService>()
                .AddTransient<PaperService>();

            services.AddTransient<UserService>();
            services.AddTransient<AuthenticationService>();
            services.AddTransient<UserCodeService>();
            services.AddIdentity<User, Role>()
                .AddEntityFrameworkStores<ApplicationIdentityDbContext>();
            services.AddLogging();
            
            IConfiguration configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.test.json")
                .Build();
            services.AddSingleton(configuration);

            var _provider = services.BuildServiceProvider();
            var identityDbContext = _provider.GetRequiredService<ApplicationIdentityDbContext>();
            var vxDbContext = _provider.GetRequiredService<TraceabilityTestDbContext>();
            var appDbContext = _provider.GetRequiredService<ApplicationDbContext>();

            identityDbContext.Database.EnsureDeleted();
            appDbContext.Database.EnsureDeleted();
            vxDbContext.Database.EnsureDeleted();

            return _provider;
        }
    }
}