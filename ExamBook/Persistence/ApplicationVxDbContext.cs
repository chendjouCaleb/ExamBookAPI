using Microsoft.EntityFrameworkCore;
using Vx.EFCore;

namespace ExamBook.Persistence
{
    public class ApplicationVxDbContext:VxDbContext
    {
        public ApplicationVxDbContext(DbContextOptions<ApplicationVxDbContext> options) : base(options)
        {
        }

        public ApplicationVxDbContext()
        {
        }
    }
}