namespace ExamBook.Helpers
{
    public class StringHelper
    {
        public static string Normalize(string value)
        {
            return value.Normalize().ToUpperInvariant();
        }
    }
}