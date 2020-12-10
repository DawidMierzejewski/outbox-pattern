using System;

namespace Outbox.Examples
{
    public class SimpleMessage
    {
        public Guid Id { get; set; }

        public bool IsTestMessage { get; } = true;
    }
}
