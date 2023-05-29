namespace ExamBook.Exceptions
{
    public class ElementNotFoundException:BaseException
    {
        public ElementNotFoundException(string code, params object[] parameters) : base(code, parameters) {}
    }
}