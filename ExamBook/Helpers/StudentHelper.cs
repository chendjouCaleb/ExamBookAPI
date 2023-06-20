using System;
using ExamBook.Entities;

namespace ExamBook.Helpers
{
    public class StudentHelper
    {
        public static void ThrowStudentNotFound(Space space, string rId)
        {
            var m = $"Student with rId: {rId} not found in space: {space.Name}.";
            throw new InvalidOperationException(m);
        }
        
        public static void ThrowDuplicateRId(Space space, string rId)
        {
            var m = $"The rId: {rId} is already used  in space: {space.Name}.";
            throw new InvalidOperationException(m);
        }
        
        public static void ThrowDuplicateStudentSpeciality(Speciality speciality, Student student)
        {
            var m = $"The Student: {student.Code} is already found in speciality: {speciality!.Name} in" +
                    $" space: {speciality.Space!.Name}.";
            throw new InvalidOperationException(m);
        }
    }
}