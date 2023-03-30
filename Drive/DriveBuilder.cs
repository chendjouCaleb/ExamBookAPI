using System;
using System.IO;
using DriveIO.EFCore;
using DriveIO.LocalStores;
using DriveIO.Repositories;
using DriveIO.Stores;
using Microsoft.Extensions.DependencyInjection;

namespace DriveIO
{
    public class DriveBuilder
    {
        private readonly IServiceCollection _services;

        public DriveBuilder(IServiceCollection services)
        {
            _services = services;
        }

        public DriveBuilder AddEntityFrameworkStores<T>() where T:DriveDbContext
        {
            _services.AddTransient<IPictureRepository, PictureEFRepository<T>>();
            _services.AddTransient<IVideoRepository, VideoEFRepository<T>>();
            _services.AddTransient<IFileRepository, FileEFRepository<T>>();
            _services.AddTransient<IFolderRepository, FolderEFRepository<T>>();
            return this;
        }

        public DriveBuilder AddLocalFileStores(Action<LocalFileStoreOptions> optionAction)
        {
            var options = new LocalFileStoreOptions();
            optionAction.Invoke(options);

            if (string.IsNullOrWhiteSpace(options.DirectoryPath))
            {
                throw new InvalidOperationException("Directory path should not be empty or null.");
            }

            if (!Path.Exists(options.DirectoryPath))
            {
                throw new InvalidOperationException($"The dir '{options.DirectoryPath}' does not exists.");
            }

            _services.AddSingleton(options);
            _services.AddTransient<IFileStore, LocalFileStore>();
            _services.AddTransient<IFolderStore, LocalFolderStore>();
            return this;
        }
    }
}