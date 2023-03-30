using Microsoft.EntityFrameworkCore;
using Social.EFCore;

namespace ExamBook.Persistence
{
    public class ApplicationSocialDbContext:SocialDbContext
    {
        public ApplicationSocialDbContext(DbContextOptions<ApplicationSocialDbContext> options) : base(options)
        {
        }

        public ApplicationSocialDbContext()
        {
        }
    }
}