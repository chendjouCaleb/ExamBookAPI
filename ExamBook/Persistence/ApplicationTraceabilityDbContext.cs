using Microsoft.EntityFrameworkCore;
using Traceability.EFCore;

namespace ExamBook.Persistence
{
    public class ApplicationTraceabilityDbContext:TraceabilityDbContext
    {
        public ApplicationTraceabilityDbContext(DbContextOptions<ApplicationTraceabilityDbContext> options) : base(options)
        {
        }

        public ApplicationTraceabilityDbContext()
        { }
    }
}