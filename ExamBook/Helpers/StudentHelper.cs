using System;
using ExamBook.Entities;

namespace ExamBook.Helpers
{
    public class StudentHelper
    {
        public static void ThrowStudentNotFound(Classroom classroom, string rId)
        {
            var m = $"Student with rId: {rId} not found in classroom: {classroom.Name}.";
            throw new InvalidOperationException(m);
        }
        
        public static void ThrowDuplicateRId(Classroom classroom, string rId)
        {
            var m = $"The rId: {rId} is already used  in classroom: {classroom.Name}.";
            throw new InvalidOperationException(m);
        }
        
        public static void ThrowDuplicateStudentSpeciality(ClassroomSpeciality speciality, Student student)
        {
            var m = $"The Student: {student.RId} is already found in speciality: {speciality.Speciality!.Name} in" +
                    $" classroom: {speciality.Classroom!.Name}.";
            throw new InvalidOperationException(m);
        }
    }
}