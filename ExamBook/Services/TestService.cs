using System;
using System.Threading.Tasks;
using ExamBook.Entities;
using ExamBook.Helpers;
using ExamBook.Models;
using ExamBook.Utils;
using Microsoft.EntityFrameworkCore;

namespace ExamBook.Services
{
    public class TestService
    {
        private readonly DbContext _dbContext;


        public async Task<Test> Add(Examination examination, TestAddModel model)
        {
            Asserts.NotNull(examination, nameof(examination));
            Asserts.NotNull(model, nameof(model));

            if (await ContainsAsync(examination, model.Name))
            {
                TestHelper.ThrowNameUsed(examination, model.Name);
            }

            Test test = new()
            {
                Name = model.Name,
                StartAt = model.StartAt,
                Coefficient = model.Coefficient,
                Radical = model.Radical,
                Duration = model.Duration,
            };
            await _dbContext.AddAsync(test);
            await _SetRoomAsync(test, model.RoomId);

            
            await _dbContext.SaveChangesAsync();

            return test;
        }

        private async Task _SetRoomAsync(Test test, ulong roomId)
        {
            Asserts.NotNull(test, nameof(test));
            
            
            var room = await _dbContext.Set<Room>().FindAsync(roomId);
            if (room == null)
            {
                throw new ArgumentNullException(nameof(room));
            }
            
            if (test.Examination.SpaceId != room.SpaceId)
            {
                throw new InvalidOperationException("Incompatibles entities.");
            }

            test.Room = room;
        }

        public async Task<bool> ContainsAsync(Examination examination, string name)
        {
            Asserts.NotNull(examination, nameof(examination));
            Asserts.NotNullOrWhiteSpace(name, nameof(name));

            string normalized = name.Normalize().ToUpper();
            return await _dbContext.Set<Test>()
                .AnyAsync(p => examination.Equals(p.Examination) && p.Name == normalized);
        }
    }
}