using System;

namespace DriveIO.Helpers
{
    public class StringHelper
    {
        public static string Normalize(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                throw new ArgumentNullException(nameof(value));
            }

            return value.Normalize().ToUpperInvariant();
        }
    }
}