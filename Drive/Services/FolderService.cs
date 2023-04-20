using System;
using System.Threading.Tasks;
using DriveIO.Helpers;
using DriveIO.Models;
using DriveIO.Repositories;
using DriveIO.Stores;
using Microsoft.Extensions.Logging;


namespace DriveIO.Services
{
    public class FolderService
    {
        private readonly IFolderStore _folderStore;
        private readonly IFolderRepository _folderRepository;
        private readonly ILogger<FolderService> _logger;

        public FolderService(IFolderStore folderStore, IFolderRepository folderRepository, ILogger<FolderService> logger)
        {
            _folderStore = folderStore;
            _folderRepository = folderRepository;
            _logger = logger;
        }


        public async Task<Folder> FindByNameAsync(string name)
        {
            var folder = await _folderRepository.FindByNameAsync(name);

            if (folder == null)
            {
                throw new InvalidOperationException($"Folder with name:'{name}' not found.");
            }
            return folder;
        }

        public async Task<Folder> CreateIfNotExistsAsync(string name)
        {
            Folder? folder = await _folderRepository.FindByNameAsync(name);

            if (folder != null)
            {
                await _folderStore.CreateIfNotExistsAsync(name);
                return folder;
            }

            return await CreateFolderAsync(name);
        }

        public async Task<Folder> CreateFolderAsync(string name)
        {
            string normalizedName = StringHelper.Normalize(name);
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new InvalidOperationException($"Cannot create folder with empty name.");
            }

            if (await _folderRepository.ContainsByNameAsync(name))
            {
                throw new InvalidOperationException($"Folder with name: '{name}' already exists en DB.");
            }

            if (await _folderStore.ContainsAsync(name))
            {
                throw new InvalidOperationException($"There are already folder with name: '{name}' is store.");
            }

            Folder folder = new()
            {
                Name = name,
                NormalizedName = normalizedName
            };
            await _folderStore.CreateAsync(name);
            await _folderRepository.AddAsync(folder);
            _logger.LogInformation("New folder: {}", name);
            return folder;
        }

        public async Task DeleteFolder(Folder folder)
        {
            Asserts.NotNull(folder, nameof(folder));

            if (!await _folderStore.ContainsAsync(folder.Name))
            {
               
            }

            await _folderRepository.DeleteAsync(folder);

            if (await _folderStore.ContainsAsync(folder.Name))
            {
                await _folderStore.DeleteAsync(folder.Name);
            }
        }

    }
}