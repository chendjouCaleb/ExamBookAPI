using System;
using System.Linq;
using System.Threading.Tasks;
using ExamBook.Exceptions;
using ExamBook.Identity.Models;
using ExamBook.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ExamBook.Identity
{
    public class AuthenticationService
    {
        private readonly ApplicationIdentityDbContext _dbContext;
        private readonly UserService _userService;

        public AuthenticationService(ApplicationIdentityDbContext dbContext)
        {
            _dbContext = dbContext;
        }


        public async Task<Session> Login(LoginModel model)
        {
            var user = await _userService.FindByIdAsync(model.Id);

            if (user != null)
            {
                
            }
            
            
            throw new NotImplementedException();
        }


        public async Task LogOut(string sessionId)
        {
            var session = await GetByIdAsync(sessionId);
            session.EndAt = DateTime.Now;
            _dbContext.Update(session);
            await _dbContext.SaveChangesAsync();
        }

        public async Task<Session> GetByIdAsync(string id)
        {
            var session = await _dbContext.Sessions
                .Where(s => s.Id == id)
                .FirstOrDefaultAsync();

            if (session == null)
            {
                throw new ElementNotFoundException("SessionNotFound");
            }
            
            return session;
        }
    }
}