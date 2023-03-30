using ExamBook.Identity.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace ExamBook.Persistence
{
    public class ApplicationIdentityDbContext:IdentityDbContext<User>
    {
        public ApplicationIdentityDbContext(DbContextOptions<ApplicationIdentityDbContext> options):base(options)
        {
            
        }
        
        public DbSet<User> Users => Set<User>();
        public DbSet<Role> Roles => Set<Role>();
        public DbSet<Session> Sessions => Set<Session>();
    }
}