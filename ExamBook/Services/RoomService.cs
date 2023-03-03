using System;
using System.Linq;
using System.Threading.Tasks;
using ExamBook.Entities;
using ExamBook.Helpers;
using ExamBook.Identity.Models;
using ExamBook.Models;
using ExamBook.Utils;
using Microsoft.EntityFrameworkCore;

namespace ExamBook.Services
{
    public class RoomService
    {
        private readonly DbContext _dbContext;

        public RoomService(DbContext dbContext)
        {
            _dbContext = dbContext;
        }


        public async Task<Room> AddRoomAsync(Space space, RoomAddModel model)
        {
            Asserts.NotNull(space, nameof(space));
            Asserts.NotNull(model, nameof(model));
            
            if (await ContainsAsync(space, model.Name))
            {
                RoomHelper.ThrowNameUsed(space, model.Name);
            }

            if (model.Capacity < 5)
            {
                RoomHelper.ThrowMinimalCapacityError(5);
            }

            Room room = new()
            {
                Name = model.Name,
                Capacity = model.Capacity,
                Space = space
            };

            _dbContext.Add(room);
            await _dbContext.SaveChangesAsync();
            return room;
        }

        public async Task ChangeNameAsync(Room room, RoomChangeNameModel model)
        {
            Asserts.NotNull(room.Space, nameof(room.Space));
            
            if (await ContainsAsync(room.Space, model.Name))
            {
                RoomHelper.ThrowNameUsed(room.Space, model.Name);
            }

            room.Name = model.Name;
            _dbContext.Update(room);
            await _dbContext.SaveChangesAsync();
        }
        
        public async Task ChangeCapacityAsync(Room room, RoomChangeCapacityModel model)
        {
            Asserts.NotNull(room.Space, nameof(room.Space));
            Asserts.NotNull(model, nameof(model));
            
            if (model.Capacity < 5)
            {
                RoomHelper.ThrowMinimalCapacityError(5);
            }

            room.Capacity = model.Capacity;
            _dbContext.Update(room);
            await _dbContext.SaveChangesAsync();
        }

        public async Task<bool> ContainsAsync(Space space, string name)
        {
            return await _dbContext.Set<Room>()
                .Where(r => r.Name.Equals(name, StringComparison.OrdinalIgnoreCase) && space.Id == r.SpaceId)
                .AnyAsync();
        }
        
        public async Task<Room?> FindAsync(Space space, string name)
        {
            return await _dbContext.Set<Room>()
                .Where(r => r.Name.Equals(name, StringComparison.OrdinalIgnoreCase) && space.Id == r.SpaceId)
                .FirstOrDefaultAsync();
        }

        public async Task DeleteAsync(Room room, User user)
        {
            room.Capacity = 0;
            room.Name = "";
            
            room.DeletedAt = DateTime.UtcNow;
            room.DeletedById = user.Id;

            _dbContext.Update(room);
            await _dbContext.SaveChangesAsync();
        }
    }
}