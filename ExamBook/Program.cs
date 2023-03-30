using System.Text;
using DriveIO;
using DriveIO.Repositories;
using DriveIO.Services;
using ExamBook.Identity;
using ExamBook.Persistence;
using ExamBook.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;
using Social;

var builder = WebApplication.CreateBuilder(args);

var services = builder.Services;
var configuration = builder.Configuration;

services.AddControllers()
    .AddNewtonsoftJson(options =>
    {
        options.SerializerSettings.NullValueHandling = NullValueHandling.Ignore;
        options.SerializerSettings.DateFormatHandling = DateFormatHandling.IsoDateFormat;
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
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(configuration["Jwt:Key"])),
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

services.AddDrive(options =>
{
    
}).AddEntityFrameworkStores<ApplicationDriveDbContext>()
    .AddLocalFileStores(options =>
    {
        options.DirectoryPath = "E:/Lab/Drive/ExamBook";
    });



var app = builder.Build();

var folderService = app.Services.CreateScope().ServiceProvider.GetRequiredService<FolderService>();
await folderService.CreateFolder("images");
await folderService.CreateFolder("videos");
await folderService.CreateFolder("gifs");
await folderService.CreateFolder("audios");

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