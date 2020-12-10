using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Outbox.EntityFramework.Entities;
using Outbox.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace Outbox.EntityFramework
{
    public class EntityFrameworkOutboxSender<TDbContext, TEntity> : IOutboxSender
            where TDbContext : DbContext
            where TEntity : class, IOutboxEntityMessage, new()
    {
        private readonly TDbContext _dbContext;
        private readonly IBusPublisher _busPublisher;
        private readonly ILogger<EntityFrameworkOutboxSender<TDbContext, TEntity>> _logger;

        public EntityFrameworkOutboxSender(TDbContext dbContext,
            IBusPublisher busPublisher,
            ILogger<EntityFrameworkOutboxSender<TDbContext, TEntity>> logger)
        {
            _dbContext = dbContext;
            _busPublisher = busPublisher;
            _logger = logger;
        }

        public async Task<MessagePublicationResult> PublishUnsentMessagesAsync(int count, CancellationToken cancellationToken = default)
        {
            var unsentMessages = await GetUnsentMessagesAsync(count, cancellationToken);

            var processedMessages = new List<ProcessedMessage>();

            foreach (var unsentMessage in unsentMessages.OrderBy(message => message.OccurredOn))
            {
                try
                {
                    var messageType = GetMessageType(unsentMessage.AssemblyName, unsentMessage.FullNameMessageType);
                    if (messageType == default)
                    {
                        _logger.LogWarning($"Message Type can not be found {unsentMessage.AssemblyName}, {unsentMessage.FullNameMessageType}");
                        continue;
                    }

                    bool isSent = await SendMessage(unsentMessage.OutboxMessageId, messageType, cancellationToken);
                    if (isSent)
                    {
                        processedMessages.Add(new ProcessedMessage {
                              MessageId = unsentMessage.OutboxMessageId,
                              IsPublished = true,
                          });
                    }
                    else
                    {
                        processedMessages.Add(new ProcessedMessage {
                              MessageId = unsentMessage.OutboxMessageId,
                              IsPublished = false,
                              PublishedByAnotherProcess = true
                          });
                    }
                }
                catch (DbUpdateConcurrencyException e)
                {
                    _logger.LogInformation(e, $"Outbox - published by another process, Message id: {unsentMessage.MessageId}, OutboxMessage Id: {unsentMessage.OutboxMessageId}");
                    
                    processedMessages.Add(new ProcessedMessage { 
                            MessageId = unsentMessage.OutboxMessageId, 
                            IsPublished = false,
                            PublishedByAnotherProcess = true 
                        });
                }
                catch (Exception e)
                {
                    _logger.LogError(e, $"Outbox - publish failed, Message id: {unsentMessage.MessageId}, OutboxMessage Id: {unsentMessage.OutboxMessageId}");

                    processedMessages.Add(new ProcessedMessage { 
                            MessageId = unsentMessage.OutboxMessageId, 
                            IsPublished = false 
                        });
                }
            }

            return new MessagePublicationResult
            {
                PublishedMessagesCount = processedMessages.Count(p => p.IsPublished),
                PublishedByAnotherProcess = processedMessages.Count(p => p.PublishedByAnotherProcess),
                UnpublishedMessagesCount = processedMessages.Count(p => !p.IsPublished)
            };
        }

        private async Task<bool> SendMessage(long messageId, Type messageType, CancellationToken cancellationToken = default)
        {
            var outboxMessagesSet = _dbContext.Set<TEntity>();

            using (var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken))
            {
                var message = await outboxMessagesSet.SingleOrDefaultAsync(o => o.OutboxMessageId == messageId);
                if (message.SentDate != null)
                {
                    await transaction.CommitAsync(cancellationToken);
                    return false;
                }

                message.SentDate = DateTime.Now;

                outboxMessagesSet.Update(message);

                await _dbContext.SaveChangesAsync(cancellationToken);

                var deserializedMesssage = JsonConvert.DeserializeObject(message.SerializedMessage, messageType);
                await _busPublisher.PublishAsync(deserializedMesssage, cancellationToken);

                await transaction.CommitAsync(cancellationToken);

                return true;
            }
        }

        private async Task<IEnumerable<TEntity>> GetUnsentMessagesAsync(int count, CancellationToken cancellationToken = default)
        {
            var outboxMessagesSet = _dbContext.Set<TEntity>();

            var outboxMessages = await outboxMessagesSet
                .Where(om => om.SentDate == null)
                .Take(count)
                .AsNoTracking()
                .ToListAsync(cancellationToken);

            return outboxMessages;
        }

        private Type GetMessageType(string assemblyName, string fullNameMessageType)
        {
            return
                Assembly
                    .Load(assemblyName)
                    .GetTypes().Single(t => t.FullName != null && t.IsClass && t.FullName.Equals(fullNameMessageType));
        }

        private class ProcessedMessage
        {
            public long MessageId { get; set; }

            public bool IsPublished { get; set; }

            public bool PublishedByAnotherProcess { get; set; }
        }
    }
}
