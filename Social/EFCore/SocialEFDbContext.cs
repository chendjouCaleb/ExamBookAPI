using Microsoft.EntityFrameworkCore;
using Social.Entities;

namespace Social.EFCore
{
    public class SocialDbContext:DbContext
    {
        public SocialDbContext(DbContextOptions<SocialDbContext> options): base(options) {}
        public SocialDbContext(DbContextOptions options): base(options) {}
        
        public SocialDbContext() {}
        
        public DbSet<Post> Posts => Set<Post>();
        public DbSet<Reaction> Reactions => Set<Reaction>();
        public DbSet<Repost> Reposts => Set<Repost>();
        public DbSet<Author> Authors => Set<Author>();
        public DbSet<PostFile> PostFiles => Set<PostFile>();

        public DbSet<AuthorSubscription> AuthorSubscriptions => Set<AuthorSubscription>();
    }
}