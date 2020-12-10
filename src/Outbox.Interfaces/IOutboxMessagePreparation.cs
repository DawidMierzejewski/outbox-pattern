using System.Threading;
using System.Threading.Tasks;

namespace Outbox.Interfaces
{
    public interface IOutboxMessagePreparation
    {
        Task PrepareMessageToPublishAsync<T>(T message, string objectId, CancellationToken cancellationToken = default);
    }
}
