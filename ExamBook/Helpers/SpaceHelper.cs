using System;

namespace ExamBook.Helpers
{
    public static class SpaceHelper
    {
        public static void ThrowDuplicateSpeciality()
        {
            throw new InvalidOperationException("The provided name of speciality is already used.");
        }
        
        public static void ThrowDuplicateClassroom()
        {
            throw new InvalidOperationException("The provided name of classroom is already used.");
        }
        
        
        public static void ThrowDuplicateClassroomSpeciality()
        {
            throw new InvalidOperationException("The speciality already present in classroom.");
        }
    }
}