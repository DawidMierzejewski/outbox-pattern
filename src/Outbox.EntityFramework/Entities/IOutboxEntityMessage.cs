using System;

namespace Outbox.EntityFramework.Entities
{
    public interface IOutboxEntityMessage
    {
        long OutboxMessageId { get; set; }
        string ObjectId { get; set; }
        Guid MessageId { get; set; }
        string SerializedMessage { get; set; }
        string AssemblyName { get; set; }

        string FullNameMessageType { get; set; }

        DateTime? SentDate { get; set; }
        DateTime OccurredOn { get; set; }
    }
}
