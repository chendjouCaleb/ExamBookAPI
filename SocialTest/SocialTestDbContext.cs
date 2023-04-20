using Microsoft.EntityFrameworkCore;
using Social.EFCore;

namespace SocialTest
{
    public class SocialTestDbContext : SocialDbContext
    {
        public SocialTestDbContext(DbContextOptions<SocialTestDbContext> options) : base(options)
        {
        }

        public SocialTestDbContext()
        {
        }
    }
}