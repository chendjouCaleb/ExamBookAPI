using System;

namespace Traceability.Models
{
	public class Subject
	{
		public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
		public string Id { get; set; } = Guid.NewGuid().ToString();
	}
}