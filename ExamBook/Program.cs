using ExamBook.Persistence;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;

var builder = WebApplication.CreateBuilder(args);

var services = builder.Services;

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

    
    var connectionStrings = builder.Configuration["Data:ConnectionStrings"];
    var version = ServerVersion.AutoDetect(connectionStrings);
    options.UseMySql(connectionStrings, version);
});

var app = builder.Build();


app.MapControllers();
app.Run();