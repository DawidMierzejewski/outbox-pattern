using System;

namespace Outbox.EntityFramework.Entities
{
    public class OutboxMessage : IOutboxEntityMessage
    {
        public long OutboxMessageId { get; set; }

        public Guid MessageId { get; set; }

        public string ObjectId { get; set; }

        public string SerializedMessage { get; set; }

        public DateTime OccurredOn { get; set; }

        public DateTime? SentDate { get; set; }

        public string AssemblyName { get; set; }

        public string FullNameMessageType { get; set; }
    }
}
