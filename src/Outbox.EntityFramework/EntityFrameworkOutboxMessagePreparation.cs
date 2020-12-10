using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using Outbox.EntityFramework.Entities;
using Outbox.Interfaces;

namespace Outbox.EntityFramework
{

    public class EntityFrameworkOutboxMessagePreparation<TDbContext, TEntity> : IOutboxMessagePreparation
        where TDbContext : DbContext
        where TEntity : class, IOutboxEntityMessage, new()
    {
        private readonly TDbContext _dbContext;

        public EntityFrameworkOutboxMessagePreparation(TDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task PrepareMessageToPublishAsync<T>(T message, string objectId, CancellationToken cancellationToken = default)
        {
            var outboxMessageEntity = _dbContext.Set<TEntity>();

            await outboxMessageEntity.AddAsync(new TEntity
            {
                ObjectId = objectId,
                MessageId = Guid.NewGuid(),
                SerializedMessage = JsonConvert.SerializeObject(message),
                FullNameMessageType = message.GetType().FullName,
                AssemblyName = message.GetType().Assembly.GetName().Name
            }, cancellationToken);

            await _dbContext.SaveChangesAsync(cancellationToken);
        }
    }
}
