using System.Text.RegularExpressions;

namespace ExamBook.Helpers
{
    public class StringHelper
    {
        public static readonly Regex GuidRegex =
            new(
                "^(?:\\{{0,1}(?:[0-9a-fA-F]){8}-(?:[0-9a-fA-F]){4}-(?:[0-9a-fA-F]){4}-(?:[0-9a-fA-F]){4}-(?:[0-9a-fA-F]){12}\\}{0,1})$");
        public static string Normalize(string value)
        {
            return value.Normalize().ToUpperInvariant();
        }

        public static bool IsGuid(string value)
        {
            return GuidRegex.IsMatch(value);
        }
    }
}