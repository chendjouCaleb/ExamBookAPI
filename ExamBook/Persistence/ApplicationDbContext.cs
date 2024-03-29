﻿using ExamBook.Entities;
using Microsoft.EntityFrameworkCore;

namespace ExamBook.Persistence
{
    public class ApplicationDbContext:DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options):base(options)
        {
            
        }
        
        public DbSet<Course> Courses { get; set; }
        public DbSet<CourseClassroom> CourseClassrooms { get; set; }
        public DbSet<CourseHour> CourseHours { get; set; }
        public DbSet<CourseSession> CourseSessions { get; set; }
        public DbSet<CourseSpeciality> CourseSpecialities { get; set; }
        public DbSet<CourseTeacher> CourseTeachers { get; set; }
        
        public DbSet<Student> Students { get; set; }
        public DbSet<StudentSpeciality> StudentSpecialities => Set<StudentSpeciality>();

        public DbSet<Examination> Examinations { get; set; } = null!;
        public DbSet<ExaminationSpeciality> ExaminationSpecialities { get; set; }
        
        
        public DbSet<Member> Members { get; set; }
        public DbSet<Paper> Papers { get; set; }
        public DbSet<PaperScore> PaperScores => Set<PaperScore>();
        public DbSet<Participant> Participants { get; set; }
        public DbSet<ParticipantSpeciality> ParticipantSpecialities { get; set; }
        public DbSet<Room> Rooms { get; set; }
        public DbSet<Test> Tests { get; set; }
        public DbSet<TestTeacher> TestTeachers => Set<TestTeacher>();
        public DbSet<TestGroup> TestGroups { get; set; }
        public DbSet<TestSpeciality> TestSpecialities { get; set; }
        
        
        
        public DbSet<Space> Spaces { get; set; }
        public DbSet<Speciality> Specialities { get; set; }
        public DbSet<Classroom> Classrooms { get; set; }
        public DbSet<ClassroomSpeciality> ClassroomSpecialities => Set<ClassroomSpeciality>();
        

    }
}