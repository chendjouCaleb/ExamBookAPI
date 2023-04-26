using System.Collections.Generic;
using ExamBook.Entities;

namespace ExamBook.Models
{
    public class CourseAddModel
    {
        public string Name { get; set; } = "";
        public string Code { get; set; } = "";
        public string Description { get; set; } = "";
        public uint Coefficient { get; set; }

        public List<CourseTeacherAddModel> CourseTeacherAddModels { get; set; } = new();
        public HashSet<ulong> SpecialityIds { get; set; } = new();
    }

    public class CourseChangeCode
    {
        public string Code { get; set; } = "";
    }

    public class CourseChangeInfo
    {
        public string Name { get; set; } = "";
        public string Description { get; set; } = "";
        public uint Coefficient { get; set; }
    }

    public class CourseTeacherAddModel
    {
        public bool IsPrincipal { get; set; }
        public ulong MemberId { get; set; }
    }

    public class CourseSpecialityAddModel
    {
        public ulong CourseId { get; set; }
        public ulong SpecialityId { get; set; }
    }
}