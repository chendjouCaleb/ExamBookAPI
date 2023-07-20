using Microsoft.EntityFrameworkCore;
using Traceability.Models;

namespace Traceability.EFCore
{
    public class TraceabilityDbContext:DbContext
    {
        protected TraceabilityDbContext()
        { }

        public TraceabilityDbContext(DbContextOptions options) : base(options)
        { }

        public DbSet<Publisher> Publishers => Set<Publisher>();
        public DbSet<PublisherEvent> PublisherEvents => Set<PublisherEvent>();
        public DbSet<Actor> Actors => Set<Actor>();
        public DbSet<Event> Events => Set<Event>();
        public DbSet<Subscription> Subscription => Set<Subscription>();
        public DbSet<Subject> Subjects => Set<Subject>();
    }
}