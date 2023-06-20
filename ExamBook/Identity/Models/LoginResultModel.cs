
using ExamBook.Identity.Entities;

namespace ExamBook.Identity.Models
{
    public class LoginResultModel
    {
        public LoginResultModel(Session session, string jwtToken)
        {
            Session = session;
            JwtToken = jwtToken;
        }

        public Session Session { get; set; }
        public string JwtToken { get; set; }
    }
}