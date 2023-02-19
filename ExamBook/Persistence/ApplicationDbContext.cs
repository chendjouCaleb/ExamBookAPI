using ExamBook.Entities;
using Microsoft.EntityFrameworkCore;

namespace ExamBook.Persistence
{
    public class ApplicationDbContext:DbContext
    {
        public DbSet<Examination> Examinations { get; set; } = null!;
        public DbSet<Space> Groups { get; set; }
        public DbSet<Member> Members { get; set; }
        public DbSet<Paper> Papers { get; set; }
        public DbSet<Participant> Participants { get; set; }
        public DbSet<Room> Rooms { get; set; }
        public DbSet<Test> Tests { get; set; }
    }
}