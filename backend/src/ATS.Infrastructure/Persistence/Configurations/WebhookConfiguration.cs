using ATS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ATS.Infrastructure.Persistence.Configurations;

public class WebhookSubscriptionConfiguration : IEntityTypeConfiguration<WebhookSubscription>
{
    public void Configure(EntityTypeBuilder<WebhookSubscription> builder)
    {
        builder.Property(w => w.Url).IsRequired().HasMaxLength(2000);
        builder.Property(w => w.Secret).IsRequired();
        builder.Property(w => w.EventTypesCsv).IsRequired();

        builder.HasOne(w => w.Company).WithMany()
            .HasForeignKey(w => w.CompanyId).OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(w => w.Deliveries).WithOne(d => d.WebhookSubscription)
            .HasForeignKey(d => d.WebhookSubscriptionId).OnDelete(DeleteBehavior.Cascade);
    }
}

public class WebhookDeliveryLogConfiguration : IEntityTypeConfiguration<WebhookDeliveryLog>
{
    public void Configure(EntityTypeBuilder<WebhookDeliveryLog> builder)
    {
        builder.Property(d => d.EventType).IsRequired().HasMaxLength(100);
        builder.Property(d => d.PayloadJson).IsRequired();
    }
}
