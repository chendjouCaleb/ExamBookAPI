using ExamBook.Entities;
using Microsoft.Extensions.DependencyInjection;

namespace ExamBook.Services
{
    public static class ApplicationServiceConfiguration
    {
        public static void AddApplicationServices(this IServiceCollection collection)
        {
            collection.AddTransient<CourseService>();
            collection.AddTransient<CourseHourService>();
            collection.AddTransient<CourseSession>();
            collection.AddTransient<CourseSessionService>();
            collection.AddTransient<CourseHourService>();
            collection.AddTransient<CourseTeacherService>();
            collection.AddTransient<ExaminationService>();
            collection.AddTransient<MemberService>();
            collection.AddTransient<PaperService>();
            
            collection.AddTransient<ParticipantService>();
            collection.AddTransient<ParticipantSpecialityService>();
            
            collection.AddTransient<RoomService>();
            collection.AddTransient<SpaceService>();
            collection.AddTransient<SpecialityService>();
            collection.AddTransient<StudentService>();
            collection.AddTransient<StudentSpecialityService>();
            collection.AddTransient<TestGroupService>();
            collection.AddTransient<TestSpecialityService>();
            collection.AddTransient<TestService>();
        }
    }
}