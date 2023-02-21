using System.Linq;
using System.Threading.Tasks;
using ExamBook.Entities;
using ExamBook.Exceptions;
using ExamBook.Helpers;
using ExamBook.Utils;
using Microsoft.EntityFrameworkCore;

namespace ExamBook.Services
{
    public class TestGroupService
    {
        public readonly DbContext _dbContext;


        public async Task<TestGroup> Add(Test test, Room room)
        {
            Asserts.NotNull(test, nameof(test));
            Asserts.NotNull(room, nameof(room));
            
            if (await ContainsRoom(test, room))
            {
               TestHelper.ThrowDuplicateTestGroup(test, room);
            }

            if (!room.Space.Equals(test.Examination.Space))
            {
                throw new IncompatibleEntityException<Test, Room>(test, room);
            }

            var testGroup = new TestGroup
            {
                Index = await CountAsync(test),
                Test = test,
                Room = room,
                Capacity = room.Capacity
            };

            await _dbContext.AddAsync(testGroup);
            await _dbContext.SaveChangesAsync();

            return testGroup;
        }

        public async Task<uint> CountAsync(Test test)
        {
            Asserts.NotNull(test, nameof(test));
            return (uint)await _dbContext.Set<TestGroup>().Where(g => test.Equals(g.Test)).CountAsync();
        }

        public async Task<bool> ContainsRoom(Test test, Room room)
        {
            Asserts.NotNull(test, nameof(test));
            Asserts.NotNull(room, nameof(room));

            return await _dbContext.Set<TestGroup>()
                .AnyAsync(g => test.Equals(g.Test) && room.Equals(g.Room));
        }

        public async Task DeleteAsync(TestGroup testGroup)
        {
            Asserts.NotNull(testGroup, nameof(testGroup));
            _dbContext.Remove(testGroup);
            await _dbContext.SaveChangesAsync();
        }
    }
}