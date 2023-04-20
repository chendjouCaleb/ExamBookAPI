using Microsoft.EntityFrameworkCore;
using Vx.Models;

namespace Vx.EFCore
{
    public class VxDbContext:DbContext
    {
        protected VxDbContext()
        { }

        public VxDbContext(DbContextOptions options) : base(options)
        { }

        public DbSet<Publisher> Publishers => Set<Publisher>();
        public DbSet<PublisherEvent> PublisherEvents => Set<PublisherEvent>();
        public DbSet<Actor> Actors => Set<Actor>();
        public DbSet<Event> Events => Set<Event>();
        public DbSet<Subscription> Subscription => Set<Subscription>();
    }
}