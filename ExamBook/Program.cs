using System.Text;
using DriveIO;
using DriveIO.Services;
using ExamBook.Identity;
using ExamBook.Identity.Services;
using ExamBook.Persistence;
using ExamBook.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;
using Social;
using Social.Services;
using Traceability;

var builder = WebApplication.CreateBuilder(args);

var services = builder.Services;
var configuration = builder.Configuration;

services.AddControllers()
    .AddNewtonsoftJson(options =>
    {
        options.SerializerSettings.ReferenceLoopHandling = ReferenceLoopHandling.Ignore;
        options.SerializerSettings.NullValueHandling = NullValueHandling.Ignore;
        options.SerializerSettings.DateTimeZoneHandling = DateTimeZoneHandling.Utc;
    });

services.AddSignalR().AddNewtonsoftJsonProtocol();
services.AddLogging();
    

services.AddDbContext<DbContext, ApplicationDbContext>(options =>
{
    options.EnableSensitiveDataLogging();
    options.EnableDetailedErrors();

    var connectionStrings = builder.Configuration["Database:ApplicationConnectionStrings"];
    var version = ServerVersion.AutoDetect(connectionStrings);
    options.UseMySql(connectionStrings, version);
});

services.AddDbContext<ApplicationIdentityDbContext>(options =>
{
    options.EnableSensitiveDataLogging();
    options.EnableDetailedErrors();

    var connectionStrings = builder.Configuration["Database:ApplicationIdentityConnectionStrings"];
    var version = ServerVersion.AutoDetect(connectionStrings);
    options.UseMySql(connectionStrings, version);
});

services.AddDbContext<ApplicationDriveDbContext>(options =>
{
    options.EnableSensitiveDataLogging();
    options.EnableDetailedErrors();
    var connectionStrings = builder.Configuration["Database:ApplicationDriveConnectionStrings"];
    var version = ServerVersion.AutoDetect(connectionStrings);
    options.UseMySql(connectionStrings, version);
});


services.AddDbContext<ApplicationSocialDbContext>(options =>
{
    options.EnableSensitiveDataLogging();
    options.EnableDetailedErrors();
    var connectionStrings = builder.Configuration["Database:ApplicationSocialConnectionStrings"];
    var version = ServerVersion.AutoDetect(connectionStrings);
    options.UseMySql(connectionStrings, version);
});

services.AddDbContext<ApplicationTraceabilityDbContext>(options =>
{
    options.EnableSensitiveDataLogging();
    options.EnableDetailedErrors();
    var connectionStrings = builder.Configuration["Database:ApplicationVxConnectionStrings"];
    var version = ServerVersion.AutoDetect(connectionStrings);
    options.UseMySql(connectionStrings, version);
});

services.AddApplicationIdentity();
services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
}).AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidIssuer = configuration["Jwt:Issuer"],
        ValidAudience = configuration["Jwt:Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(configuration["Jwt:Key"]!)),
        ValidateIssuer = true,
        ValidateAudience = false,
        ValidateLifetime = false,
        ValidateIssuerSigningKey = true
    };
});
services.AddAuthorization(options =>
{
    
});
services.AddApplicationServices();

services.AddSocial(_ => {})
    .AddEntityFrameworkStores<ApplicationSocialDbContext>();

services.AddDrive(_ => { })
    .AddEntityFrameworkStores<ApplicationDriveDbContext>()
    .AddLocalFileStores(options =>
    {
        options.DirectoryPath = "E:/Lab/Drive/ExamBook";
    });

services.AddTraceability(_ => { })
    .AddEntityFrameworkStores<ApplicationTraceabilityDbContext>()
    .AddNewtonSoftDataSerializer(settings =>
    {
        settings.ReferenceLoopHandling = ReferenceLoopHandling.Ignore;
    });


var app = builder.Build();

var folderService = app.Services.CreateScope().ServiceProvider.GetRequiredService<FolderService>();
await folderService.CreateIfNotExistsAsync("images");
await folderService.CreateIfNotExistsAsync("videos");
await folderService.CreateIfNotExistsAsync("gifs");
await folderService.CreateIfNotExistsAsync("audios");

var authorServices =  app.Services.CreateScope().ServiceProvider.GetRequiredService<AuthorService>();
var userServices =  app.Services.CreateScope().ServiceProvider.GetRequiredService<UserService>();
await userServices.EnsureVx();
await authorServices.EnsureAuthorSelfSubscribe();
await authorServices.EnsureAuthorsHasActor();
await authorServices.EnsureAuthorsHasPublisher();

app.UseAuthentication();
app.UseAuthorization();

app.UseCors(options =>
{
    options.AllowAnyOrigin();
    options.AllowAnyHeader();
    options.AllowAnyMethod();
});

app.MapControllers();
app.Run();