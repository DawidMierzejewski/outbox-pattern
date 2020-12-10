using System.Threading;
using System.Threading.Tasks;

namespace Outbox.Interfaces
{
    public interface IOutboxSender
    {
        Task<MessagePublicationResult> PublishUnsentMessagesAsync(int count, CancellationToken cancellationToken = default);
    }
}
