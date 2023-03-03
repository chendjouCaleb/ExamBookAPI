using System;
using System.Linq;
using System.Threading.Tasks;
using ExamBook.Entities;
using ExamBook.Helpers;
using ExamBook.Models;
using ExamBook.Utils;
using Microsoft.EntityFrameworkCore;

namespace ExamBook.Services
{
    public class StudentService
    {
        private readonly DbContext _dbContext;

        public StudentService(DbContext dbContext)
        {
            _dbContext = dbContext;
        }


        public async Task<Student> Add(Classroom classroom, StudentAddModel model)
        {
            Asserts.NotNull(classroom, nameof(classroom));
            Asserts.NotNull(model, nameof(model));

            if (await ContainsAsync(classroom, model.RId))
            {
                StudentHelper.ThrowDuplicateRId(classroom, model.RId);
            }
            string normalizedRid = model.RId.Normalize().ToUpper();
            Student student = new()
            {
                FirstName = model.FirstName,
                LastName = model.LastName,
                BirthDate = model.BirthDate,
                Sex = model.Sex,
                NormalizedRId = normalizedRid,
                RId = model.RId,
                Classroom = classroom
            };
            await _dbContext.AddAsync(student);

            var classroomSpecialities = _dbContext.Set<ClassroomSpeciality>()
                .Where(e => model.ClassroomSpecialityIds.Contains(e.Id))
                .ToList();

            foreach (var classroomSpeciality in classroomSpecialities)
            {
                var studentSpeciality = await _AddSpecialityAsync(student, classroomSpeciality);
                await _dbContext.AddAsync(studentSpeciality);
            }
            
            await _dbContext.SaveChangesAsync();
            return student;
        }

        public async Task<StudentSpeciality> AddSpecialityAsync(Student student,
            ClassroomSpeciality classroomSpeciality)
        {
            var studentSpeciality = await _AddSpecialityAsync(student, classroomSpeciality);
            await _dbContext.AddAsync(studentSpeciality);
            await _dbContext.SaveChangesAsync();
            return studentSpeciality;
        }

        public async Task ChangeRId(Student student, StudentChangeRIdModel model)
        {
            Asserts.NotNull(student, nameof(student));
            Asserts.NotNull(student.Classroom, nameof(student.Classroom));
            Asserts.NotNull(model, nameof(model));
            if (await ContainsAsync(student.Classroom, model.RId))
            {
                StudentHelper.ThrowDuplicateRId(student.Classroom, model.RId);
            }
            string normalizedRid = model.RId.Normalize().ToUpper();
            student.RId = model.RId;
            student.NormalizedRId = normalizedRid;
            _dbContext.Update(student);
            await _dbContext.SaveChangesAsync();
        }


        public async Task ChangeInfo(Student student, StudentChangeInfoModel model)
        {
            Asserts.NotNull(student, nameof(student));
            Asserts.NotNull(student.Classroom, nameof(student.Classroom));
            Asserts.NotNull(model, nameof(model));

            student.Sex = model.Sex;
            student.BirthDate = model.BirthDate;
            student.FirstName = model.FirstName;
            student.LastName = model.LastName;
            _dbContext.Update(student);
            await _dbContext.SaveChangesAsync();
        }
        
        private async Task<StudentSpeciality> _AddSpecialityAsync(
            Student student,
            ClassroomSpeciality classroomSpeciality)
        {
            Asserts.NotNull(student, nameof(student));
            Asserts.NotNull(classroomSpeciality, nameof(classroomSpeciality));
            Asserts.NotNull(classroomSpeciality.Classroom, nameof(classroomSpeciality.Classroom));

            if (classroomSpeciality.ClassroomId != student.ClassroomId)
            {
                throw new InvalidOperationException("Incompatible entities.");
            }

            if (await SpecialityContainsAsync(classroomSpeciality, student))
            {
                StudentHelper.ThrowDuplicateStudentSpeciality(classroomSpeciality, student);
            }

            StudentSpeciality studentSpeciality = new()
            {
                Student = student,
                ClassroomSpeciality = classroomSpeciality
            };
            return studentSpeciality;
        }


        public async Task<bool> ContainsAsync(Classroom classroom, string rId)
        {
            Asserts.NotNull(classroom, nameof(classroom));
            Asserts.NotNullOrWhiteSpace(rId, nameof(rId));

            string normalized = rId.Normalize().ToUpper();
            return await _dbContext.Set<Student>()
                .AnyAsync(p => classroom.Equals(p.Classroom) && p.RId == normalized);
        }
        
        
        public async Task<bool> SpecialityContainsAsync(ClassroomSpeciality classroomSpeciality, string rId)
        {
            Asserts.NotNull(classroomSpeciality, nameof(classroomSpeciality));
            Asserts.NotNullOrWhiteSpace(rId, nameof(rId));

            string normalized = rId.Normalize().ToUpper();
            return await _dbContext.Set<StudentSpeciality>()
                .AnyAsync(p => classroomSpeciality.Equals(p.ClassroomSpeciality) 
                               && p.Student!.RId == normalized);
        }
        
        public async Task<bool> SpecialityContainsAsync(ClassroomSpeciality classroomSpeciality, Student student)
        {
            Asserts.NotNull(classroomSpeciality, nameof(classroomSpeciality));
            Asserts.NotNull(student, nameof(student));
            
            return await _dbContext.Set<StudentSpeciality>()
                .AnyAsync(p => classroomSpeciality.Equals(p.ClassroomSpeciality) 
                               && student.Equals(p.StudentId));
        }

        public async Task<Student?> FindAsync(Classroom classroom, string rId)
        {
            Asserts.NotNull(classroom, nameof(classroom));
            Asserts.NotNullOrWhiteSpace(rId, nameof(rId));

            string normalized = rId.Normalize().ToUpper();
            var student = await _dbContext.Set<Student>()
                .FirstOrDefaultAsync(p => classroom.Equals(p.Classroom) && p.RId == normalized);

            if (student == null)
            {
                StudentHelper.ThrowStudentNotFound(classroom, rId);
            }

            return student;
        }

        
        public async Task DeleteSpeciality(StudentSpeciality studentSpeciality)
        {
            Asserts.NotNull(studentSpeciality, nameof(studentSpeciality));
            _dbContext.Remove(studentSpeciality);
            await _dbContext.SaveChangesAsync();
        }


        public async Task MarkAsDeleted(Student student)
        {
            Asserts.NotNull(student, nameof(student));
            student.Sex = '0';
            student.BirthDate = DateTime.MinValue;
            student.FirstName = "";
            student.LastName = "";
            student.RId = "";
            student.NormalizedRId = "";
            student.DeletedAt = DateTime.Now;
            _dbContext.Update(student);
            await _dbContext.SaveChangesAsync();
        }

        public async Task Delete(Student student)
        {
           var studentSpecialities = await _dbContext.Set<StudentSpeciality>()
                .Where(p => student.Equals(p.StudentId))
                .ToListAsync();
           
           _dbContext.Set<StudentSpeciality>().RemoveRange(studentSpecialities);
           _dbContext.Set<Student>().Remove(student);
           await _dbContext.SaveChangesAsync();
        }
    }
}