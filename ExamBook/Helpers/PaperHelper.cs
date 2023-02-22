using System;
using ExamBook.Entities;

namespace ExamBook.Helpers
{
    public static class PaperHelper
    {
        
        
        public static void ThrowDuplicatePaper(Test test, Participant participant)
        {
            var m = $"There is already participant: {participant.FullName} in test: {test.Name}.";
            throw new InvalidOperationException(m);
        }
        
        public static void ThrowDuplicatePaperSpeciality()
        {
            var m = $"Try to add same speciality two time to same paper.";
            throw new InvalidOperationException(m);
        }
    }
}