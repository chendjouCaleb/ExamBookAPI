using System.ComponentModel.DataAnnotations.Schema;
using Traceability.Models;

namespace Social.Entities
{
    public class AuthorSubscription
    {
        public long Id { get; set; }
        public Author? Author { get; set; }
        public string AuthorId { get; set; } = null!;

        public string? SubscriptionId { get; set; }
        [NotMapped] public Subscription? Subscription { get; set; }
    }
}