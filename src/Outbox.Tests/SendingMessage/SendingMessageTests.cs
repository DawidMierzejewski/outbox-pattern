using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Outbox.EntityFramework;
using Outbox.EntityFramework.Entities;
using Outbox.Interfaces;
using Outbox.Tests.Context;
using System;
using System.Threading.Tasks;
using Xunit;
using System.Linq;
using System.Threading;
using Microsoft.Extensions.Configuration;

namespace Outbox.Tests.SendingMessage
{
    public class SendingMessageTests
    {
        private OutboxContext _dbContext;

        private IOutboxSender _outboxSender;

        private IOutboxMessagePreparation _outbox;

        private Mock<IBusPublisher> _publisherMock;

        public SendingMessageTests()
        {
            var config = new ConfigurationBuilder().AddJsonFile("appsettings.json").Build();

            var dbContextOptions = new DbContextOptionsBuilder<OutboxContext>();
            dbContextOptions.UseSqlServer(config.GetConnectionString("Catalog"), providerOptions => providerOptions.CommandTimeout(60));

            _dbContext = new OutboxContext(dbContextOptions.Options);

            _publisherMock = new Mock<IBusPublisher>();

            _outboxSender = CreateOutboxSender(_dbContext);

            _outbox = new EntityFrameworkOutboxMessagePreparation<OutboxContext, OutboxMessage>(_dbContext);
        }

        [Fact]
        public async Task Should_WorkInParallel()
        {
            await ClearOutboxTable();

            int messagesCount = 100;
            await PrepareMessages(messagesCount);


            Task<MessagePublicationResult> task1 = CreateOutboxSender().PublishUnsentMessagesAsync(messagesCount);
            await Task.Delay(200);

            Task<MessagePublicationResult> task2 = CreateOutboxSender().PublishUnsentMessagesAsync(messagesCount);
            await Task.Delay(400);

            Task<MessagePublicationResult> task3 = CreateOutboxSender().PublishUnsentMessagesAsync(messagesCount);

            await Task.WhenAll(task1, task2, task3);

            var result1 = await task1;
            var result2 = await task2;
            var result3 = await task3;


            VerifyPublishMethod(timesInvoked: messagesCount);

            await VerifyUnsentMessagesInDatabase(0);

            var publishedCount = result1.PublishedMessagesCount + result2.PublishedMessagesCount + result3.PublishedMessagesCount;
            publishedCount.Should().Be(messagesCount);

            (result1.PublishedByAnotherProcess + result2.PublishedByAnotherProcess + result3.PublishedByAnotherProcess)
                .Should().Be(result1.UnpublishedMessagesCount + result2.UnpublishedMessagesCount + result3.UnpublishedMessagesCount);
        }

        [Fact]
        public async Task Should_PublishAllMessages()
        {
            await ClearOutboxTable();

            var guidId = Guid.NewGuid();
            var message1 = new SimpleMessage
            {
                Id = guidId
            };

            var guidId2 = Guid.NewGuid();
            var message2 = new SimpleMessage
            {
                Id = guidId2
            };

            await _outbox.PrepareMessageToPublishAsync(message1, message1.Id.ToString());
            await _outbox.PrepareMessageToPublishAsync(message2, message2.Id.ToString());


            await _outboxSender.PublishUnsentMessagesAsync(2);


            var savedMessage1 = _dbContext.OutboxMessages.SingleOrDefault(m => m.ObjectId == guidId.ToString());
            if (savedMessage1 != null) savedMessage1.SentDate.Should().NotBeNull();

            _publisherMock.Verify(p => p.PublishAsync((object)It.Is<SimpleMessage>(
                m => m.Id == guidId), It.IsAny<CancellationToken>()));


            var savedMessage2 = _dbContext.OutboxMessages.SingleOrDefault(m => m.ObjectId == guidId2.ToString());
            if (savedMessage2 != null) savedMessage2.SentDate.Should().NotBeNull();

            _publisherMock.Verify(p => p.PublishAsync((object)It.Is<SimpleMessage>(
                m => m.Id == guidId2), It.IsAny<CancellationToken>()));

            VerifyPublishMethod(2);
        }

        [Fact]
        public async Task Should_PublishUnsentMessage()
        {
            await ClearOutboxTable();

            int messagesCount = 100;
            await PrepareMessages(messagesCount);

            int sentMessages = 40;

            var result = await _outboxSender.PublishUnsentMessagesAsync(sentMessages);
            result.PublishedMessagesCount.Should().Be(sentMessages);

            var leftMessagesCount = messagesCount - sentMessages;

            await VerifyUnsentMessagesInDatabase(leftMessagesCount);

            VerifyPublishMethod(timesInvoked: sentMessages);

       
            var result2 = await _outboxSender.PublishUnsentMessagesAsync(leftMessagesCount);

            result2.PublishedMessagesCount.Should().Be(leftMessagesCount);

            await VerifyUnsentMessagesInDatabase(0);

            VerifyPublishMethod(messagesCount);
        }

        private void VerifyPublishMethod(int timesInvoked)
        {
            _publisherMock.Verify(p => p.PublishAsync((object)It.IsAny<SimpleMessage>(), It.IsAny<CancellationToken>()),
                  Times.Exactly(timesInvoked));
        }

        private async Task VerifyUnsentMessagesInDatabase(int messagesCount)
        {
            var savedMessages = await CreateOutboxContext().OutboxMessages
                         .ToArrayAsync();

            savedMessages.Count(s => s.SentDate == null).Should().Be(messagesCount);
        }

        private OutboxContext CreateOutboxContext()
        {
            var config = new ConfigurationBuilder().AddJsonFile("appsettings.json").Build();

            var dbContextOptions = new DbContextOptionsBuilder<OutboxContext>();
            dbContextOptions.UseSqlServer(config.GetConnectionString("Catalog"), providerOptions => providerOptions.CommandTimeout(60));
            return new OutboxContext(dbContextOptions.Options);
        }

        private IOutboxSender CreateOutboxSender(OutboxContext dbContext = null)
        {
            if(dbContext == null)
            {
                dbContext = CreateOutboxContext();

            }

            return new EntityFrameworkOutboxSender<OutboxContext, OutboxMessage>(
                            dbContext,
                            _publisherMock.Object,
                            Mock.Of<ILogger<EntityFrameworkOutboxSender<OutboxContext, OutboxMessage>>>());
        }


        private async Task PrepareMessages(int count)
        {
            foreach (var iteration in Enumerable.Range(1, count))
            {
                var message = new SimpleMessage
                {
                    Id = Guid.NewGuid()
                };

                await _outbox.PrepareMessageToPublishAsync(message, message.Id.ToString());
            }
        }

        private async Task ClearOutboxTable()
        {
            _dbContext.OutboxMessages.RemoveRange(_dbContext.OutboxMessages);
            await _dbContext.SaveChangesAsync();
        }
    }
}
