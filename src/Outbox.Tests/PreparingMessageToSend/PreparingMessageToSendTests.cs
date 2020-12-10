using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Outbox.EntityFramework;
using Outbox.EntityFramework.Entities;
using Outbox.Interfaces;
using Outbox.Tests.Context;
using System;
using System.Threading.Tasks;
using Xunit;

namespace Outbox.Tests.PreparingMessageToSend
{
    public class PreparingMessageToSendTests
    {
        private OutboxContext _dbContext;
        private IOutboxMessagePreparation _outbox;

        public PreparingMessageToSendTests()
        {
            var config = new ConfigurationBuilder().AddJsonFile("appsettings.json").Build();

            var dbContextOptions = new DbContextOptionsBuilder<OutboxContext>();
            dbContextOptions.UseSqlServer(config.GetConnectionString("Catalog"), providerOptions => providerOptions.CommandTimeout(60));

            _dbContext = new OutboxContext(dbContextOptions.Options);

            _outbox = new EntityFrameworkOutboxMessagePreparation<OutboxContext, OutboxMessage>(_dbContext);
        }

        [Fact]
        public async Task Should_PrepareMessageToSend()
        {
            var guidId = Guid.NewGuid();
            var message = new SimpleMessage
            {
                Id = guidId
            };

            await _outbox.PrepareMessageToPublishAsync(message, guidId.ToString());

            var savedMessage = await _dbContext
                .OutboxMessages
                .SingleOrDefaultAsync(o => o.ObjectId == guidId.ToString());

            savedMessage.ObjectId.Should().Be(guidId.ToString());
            savedMessage.SentDate.Should().Be(null);
            savedMessage.SerializedMessage.Should().Be(JsonConvert.SerializeObject(message));
            savedMessage.FullNameMessageType.Should().Be(message.GetType().FullName);
            savedMessage.AssemblyName.Should().Be(message.GetType().Assembly.GetName().Name);
            savedMessage.MessageId.Should().NotBeEmpty();
        }
    }
}
