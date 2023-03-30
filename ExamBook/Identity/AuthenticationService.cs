using System;
using System.Threading.Tasks;
using ExamBook.Identity.Models;
using ExamBook.Persistence;

namespace ExamBook.Identity
{
    public class AuthenticationService
    {
        private readonly ApplicationIdentityDbContext _dbContext;
        


        public async Task<Session> Login(LoginModel model)
        {
            var user = await _dbContext.Set<User>()
                .FindAsync(model.Id);

            if (user != null)
            {
                
            }
            
            

            throw new NotImplementedException();
        }


        public async Task LogOut(string sessionId)
        {
            
        }
    }
}