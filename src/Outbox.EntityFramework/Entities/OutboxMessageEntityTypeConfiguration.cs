using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Outbox.EntityFramework.Entities
{
    public class OutboxMessageEntityTypeConfiguration : IEntityTypeConfiguration<OutboxMessage>
    {
        public void Configure(EntityTypeBuilder<OutboxMessage> builder)
        {
            builder.ToTable("OutboxMessages", "Outbox");

            builder.HasKey(o => o.OutboxMessageId);

            builder.Property(o => o.MessageId).IsRequired();

            builder.Property(o => o.SerializedMessage).IsRequired();

            builder.Property(o => o.FullNameMessageType).IsRequired();

            builder.Property(o => o.AssemblyName).IsRequired();

            builder.Property(e => e.OccurredOn)
                   .HasDefaultValueSql("GETDATE()")
                   .IsRequired();

            builder.Property(e => e.SentDate)
                .IsConcurrencyToken();

        }
    }
}
