namespace PublisherA.Models
{
    public class OutboxMessage
    {
        public Guid Id { get; private init; }
        public DateTime OccurredOn { get; private init; }
        public string EventType { get; init; }
        public string Payload { get; init; }

        public OutboxMessage()
        {
            Id = Guid.NewGuid();
            OccurredOn = DateTime.UtcNow;
        }
    }
}
