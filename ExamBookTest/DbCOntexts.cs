using DriveIO.EFCore;
using Microsoft.EntityFrameworkCore;
using Social.EFCore;
using Traceability.EFCore;

namespace ExamBookTest
{
    public class SocialTestDbContext : SocialDbContext
    {
        public SocialTestDbContext(DbContextOptions<SocialTestDbContext> options) : base(options) { }
        public SocialTestDbContext() { }
    }
    
    public class DriveTestDbContext : DriveDbContext
    {
        public DriveTestDbContext(DbContextOptions<DriveTestDbContext> options) : base(options) { }
        public DriveTestDbContext() { }
    } 
    
    public class TraceabilityTestDbContext : TraceabilityDbContext
    {
        public TraceabilityTestDbContext(DbContextOptions<TraceabilityTestDbContext> options) : base(options) { }
        public TraceabilityTestDbContext() { }
    } 
}