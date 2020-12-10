using Microsoft.EntityFrameworkCore;
using Outbox.EntityFramework.Entities;

namespace Outbox.Examples.OutboxContext
{
    public class OutboxContext : DbContext
    {
        public OutboxContext(DbContextOptions<OutboxContext> options) : base(options)
        {
        }

        public OutboxContext()
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.ApplyConfiguration(new OutboxMessageEntityTypeConfiguration());
        }

        public DbSet<OutboxMessage> OutboxMessages { get; set; }
    }
}
