using System;
using DriveIO.Services;
using Microsoft.Extensions.DependencyInjection;

namespace DriveIO
{
    public static class DriveConfigure
    {
        public static DriveBuilder AddDrive(this IServiceCollection services,
            Action<DriveOptions> optionAction)
        {
            var options = new DriveOptions();
            optionAction(options);
            
            var builder = new DriveBuilder(services);
            services.AddTransient<PictureService>();
            services.AddTransient<VideoService>();
            services.AddTransient<FileService>();
            services.AddTransient<FolderService>();
            services.AddSingleton(options);

            return builder;
        }
    }
}