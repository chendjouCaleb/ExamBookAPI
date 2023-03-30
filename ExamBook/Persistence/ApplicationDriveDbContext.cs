using DriveIO.EFCore;
using Microsoft.EntityFrameworkCore;

namespace ExamBook.Persistence
{
    public class ApplicationDriveDbContext:DriveDbContext
    {
        public ApplicationDriveDbContext(DbContextOptions<ApplicationDriveDbContext> options) : base(options)
        {
        }

        public ApplicationDriveDbContext()
        {
        }
    }
}