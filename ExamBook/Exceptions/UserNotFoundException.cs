namespace ExamBook.Exceptions
{
    public class UserNotFoundException:BaseException
    {
        public UserNotFoundException(string message) : base(message)
        {
        }

        public static void ThrowNotFoundUserName(string userName)
        {
            throw new UserNotFoundException($"User with userName={userName} not found.");
        }
        
        
        public static void ThrowNotFoundId(string userId)
        {
            throw new UserNotFoundException($"User with id={userId} not found.");
        }
    }
}