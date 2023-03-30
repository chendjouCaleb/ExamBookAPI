using DriveIO.Models;
using Microsoft.EntityFrameworkCore;

namespace DriveIO.EFCore
{
    public class DriveDbContext:DbContext
    {
        protected DriveDbContext()
        {
        }

        public DriveDbContext(DbContextOptions options) : base(options)
        {
        }

        public DbSet<Folder> Folders => Set<Folder>();
        public DbSet<BaseFile> Files => Set<BaseFile>();
        public DbSet<Picture> Pictures => Set<Picture>();
        public DbSet<Video> Videos => Set<Video>();
    }
}