using System;

namespace Traceability.Models
{
	public class Connection
	{
		public string Id { get; set; } = Guid.NewGuid().ToString();
		public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

		public Actor Actor { get; set; }
		public string ActorId { get; set; }
	}
}