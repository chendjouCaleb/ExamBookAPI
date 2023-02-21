using System;
using ExamBook.Entities;

namespace ExamBook.Helpers
{
    public static class ExaminationHelper
    {
        public static void ThrowPastStartAtError(DateTime startAt) {}

        public static void ThrowDuplicateNameError(Space space, string name)
        {
            
        }
        
        public static void ThrowDuplicateSpecialityNameError(Examination examination, string name)
        {
            
        }

        public static void ThrowSpecialityNotFound(Examination examination, string specialityName)
        {
            var m = $"Examination with name: {specialityName} not found in examination: {examination.Name}.";
            throw new InvalidOperationException(m);
        }
    }
}