using System;
using System.Threading.Tasks;
using ExamBook.Identity.Models;
using ExamBook.Persistence;

namespace ExamBook.Identity
{
    public class AuthenticationService
    {
        private readonly ApplicationIdentityDbContext _dbContext;

        public AuthenticationService(ApplicationIdentityDbContext dbContext)
        {
            _dbContext = dbContext;
        }


        public async Task<Session> Login(LoginModel model)
        {
            var user = await _dbContext.Set<User>()
                .FindAsync(model.Id);

            if (user != null)
            {
                
            }
            
            

            throw new NotImplementedException();
        }


        public Task LogOut(string sessionId)
        {
            throw new NotImplementedException();
        }
    }
}