using Outbox.Interfaces;
using System.Threading;
using System.Threading.Tasks;

namespace Outbox.Examples.Publisher
{
    public class MockPublisher : IBusPublisher
    {
        public Task PublishAsync<T>(T message, CancellationToken cancellationToken = default) where T : class
        {
            return Task.CompletedTask;
        }

        public Task PublishAsync(object message, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }
    }
}
