using System.Linq;
using System.Threading.Tasks;
using ExamBook.Entities;
using ExamBook.Exceptions;
using ExamBook.Helpers;
using ExamBook.Utils;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ExamBook.Services
{
    public class TestGroupService
    {
        private readonly DbContext _dbContext;
        private readonly ILogger<TestGroupService> _logger;

        public TestGroupService(DbContext dbContext, ILogger<TestGroupService> logger)
        {
            _dbContext = dbContext;
            _logger = logger;
        }


        public async Task<TestGroup> Add(Test test, Room room)
        {
            AssertHelper.NotNull(test, nameof(test));
            AssertHelper.NotNull(room, nameof(room));
            
           
            if (!room.Space.Equals(test.Examination!.Space))
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
            
            _logger.LogInformation("New Test group");

            return testGroup;
        }

        public async Task<uint> CountAsync(Test test)
        {
            AssertHelper.NotNull(test, nameof(test));
            return (uint)await _dbContext.Set<TestGroup>().Where(g => test.Equals(g.Test)).CountAsync();
        }

        public async Task<bool> ContainsRoom(Test test, Room room)
        {
            AssertHelper.NotNull(test, nameof(test));
            AssertHelper.NotNull(room, nameof(room));

            return await _dbContext.Set<TestGroup>()
                .AnyAsync(g => test.Equals(g.Test) && room.Equals(g.Room));
        }

        public async Task DeleteAsync(TestGroup testGroup)
        {
            AssertHelper.NotNull(testGroup, nameof(testGroup));
            _dbContext.Remove(testGroup);
            await _dbContext.SaveChangesAsync();
        }
    }
}