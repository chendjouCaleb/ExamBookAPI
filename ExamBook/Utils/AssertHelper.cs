using System;

namespace ExamBook.Utils
{
    public static class AssertHelper
    {
        public static void IsTrue(bool state, string message)
        {
            if (state)
            {
                throw new ArgumentException(message);
            }
        }
        
        public static void IsTrue(bool state)
        {
            if (state)
            {
                throw new ArgumentException("Should be True, but is false.");
            }
        }
        
        
        public static void IsFalse(bool state, string message)
        {
            if (state)
            {
                throw new ArgumentException(message);
            }
        }
        
        public static void NotNull(object? value, string nameOf)
        {
            if (value == null)
            {
                throw new ArgumentNullException(nameOf);
            }
        }
        
        public static void NotNullOrEmpty(string value, string nameOf)
        {
            if (string.IsNullOrEmpty(value))
            {
                throw new ArgumentNullException($"The string value: '{nameOf}' is null or empty.");
            }
        }
        
        
        public static void NotNullOrWhiteSpace(string value, string nameOf)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentNullException($"The value: '{nameOf}' is null or white space.");
            }
        }
    }
}