using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Hosting;
using Outbox.Implementation.Configuration;
using Outbox.Interfaces;

namespace Outbox.Implementation.BackgroundService
{
    public class OutboxBackgroundService : IHostedService, IDisposable
    {
        private readonly IServiceScopeFactory _serviceScopeFactory;
        private readonly ILogger<OutboxBackgroundService> _logger;
        private readonly OutboxConfiguration _outboxConfiguration;
        private Timer _timer;

        public OutboxBackgroundService(IServiceScopeFactory serviceScopeFactory,
            ILogger<OutboxBackgroundService> logger,
            OutboxConfiguration outboxConfiguration)
        {
            _serviceScopeFactory = serviceScopeFactory;
            _logger = logger;
            _outboxConfiguration = outboxConfiguration;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation($"{nameof(OutboxBackgroundService)} is running at {DateTime.Now}");

            _timer = new Timer(callback: async o => await PublishAllUnsentMessagesAsync(cancellationToken),
                state: null, dueTime: TimeSpan.FromSeconds(0),
                period: TimeSpan.FromSeconds(_outboxConfiguration.IntervalInSeconds));

            return Task.CompletedTask;
        }

        private async Task PublishAllUnsentMessagesAsync(CancellationToken cancellationToken)
        {
            using (var scope = _serviceScopeFactory.CreateScope())
            {
                var outbox = scope.ServiceProvider.GetRequiredService<IOutboxSender>();

                var processId = Guid.NewGuid();

                _logger.LogInformation($"Outbox process started, processId: {processId}, date: { DateTime.Now }");

                try
                {
                    var result = await outbox.PublishUnsentMessagesAsync(_outboxConfiguration.MessageCount, cancellationToken);

                    _logger.LogInformation(
                        $"Outbox process finished processId: {processId}, " +
                        $"published messages : {result.PublishedMessagesCount}, " +
                        $"unpublished messages: {result.UnpublishedMessagesCount}, date: {DateTime.Now}");
                }
                catch (Exception exception)
                {
                    _logger.LogError(exception, $"Outbox process failed, processId: {processId}, date: { DateTime.Now }");
                }
            }
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation($"{nameof(OutboxBackgroundService)} has been stopped at {DateTime.Now}");

            _timer?.Change(Timeout.Infinite, 0);

            return Task.CompletedTask;
        }

        public void Dispose()
        {
            _timer?.Dispose();
        }
    }
}
