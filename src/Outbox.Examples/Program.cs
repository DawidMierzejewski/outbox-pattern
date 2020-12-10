using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Outbox.EntityFramework;
using Outbox.EntityFramework.Entities;
using Outbox.Examples.Publisher;
using Outbox.Implementation.BackgroundService;
using Outbox.Implementation.Configuration;
using Outbox.Interfaces;

namespace Outbox.Examples
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var host = CreateHostBuilder(args).Build();

            await PrepareMessages(host.Services, 2);

            await host.RunAsync();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
            .ConfigureAppConfiguration((hostContext, config) =>
            {
                config
                    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                    .AddEnvironmentVariables();
            })
            .ConfigureServices((hostContext, services) =>
            {
                services.AddDbContext<OutboxContext.OutboxContext>(options =>
                      options.UseSqlServer(hostContext.Configuration.GetConnectionString("Catalog")));
              
                services.AddScoped<IBusPublisher, MockPublisher>();
                services.AddScoped<IOutboxMessagePreparation, EntityFrameworkOutboxMessagePreparation<OutboxContext.OutboxContext, OutboxMessage>>();
                services.AddScoped<IOutboxSender, EntityFrameworkOutboxSender<OutboxContext.OutboxContext, OutboxMessage>>();

                services.AddSingleton(sp => hostContext.Configuration.GetSection("OutboxConfiguration").Get<OutboxConfiguration>());

                services.AddHostedService<OutboxBackgroundService>();
            });

        private static async Task PrepareMessages(IServiceProvider serviceProvider, int count)
        {
            using (var scope = serviceProvider.CreateScope())
            {
                var outbox = scope.ServiceProvider.GetRequiredService<IOutboxMessagePreparation>();

                foreach (var item in Enumerable.Range(1, count))
                {
                    var message = new SimpleMessage
                    {
                        Id = Guid.NewGuid()
                    };

                    await outbox.PrepareMessageToPublishAsync(message, message.Id.ToString());
                }
            }
        }
    }
}
