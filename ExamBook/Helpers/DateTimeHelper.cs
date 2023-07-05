using System;

namespace ExamBook.Helpers
{
	public static class DateTimeHelper
	{
		/// <summary>
		/// Tells if this time is after other time
		/// </summary>
		/// <param name="time"></param>
		/// <param name="other"></param>
		/// <returns></returns>
		public static bool IsAfter(this DateTime time, DateTime other)
		{
			return time > other;
		}
		
		
		public static bool IsBefore(this DateTime time, DateTime other)
		{
			return time < other;
		}
		
		
	}
}