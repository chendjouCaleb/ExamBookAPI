using Microsoft.EntityFrameworkCore;
using Social.Entities;

namespace Social.EFCore
{
    public class SocialDbContext:DbContext
    {
        public SocialDbContext(DbContextOptions options): base(options) {}
        
        public SocialDbContext() {}

        public DbSet<Post> Posts => Set<Post>();
        public DbSet<Author> Authors => Set<Author>();
    }
}