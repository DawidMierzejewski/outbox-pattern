namespace Outbox.Interfaces
{
    public class MessagePublicationResult
    {
        public int PublishedMessagesCount { get; set; }

        public int PublishedByAnotherProcess { get; set; }

        public int UnpublishedMessagesCount { get; set; }
    }
}
