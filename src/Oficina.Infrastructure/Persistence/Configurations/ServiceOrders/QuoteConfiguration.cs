using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Oficina.Domain.ServiceOrders.Entities;
using Oficina.Domain.ServiceOrders.ValueObjects;
using Oficina.Infrastructure.Persistence.Converters;

namespace Oficina.Infrastructure.Persistence.Configurations.ServiceOrders;

public sealed class QuoteConfiguration : IEntityTypeConfiguration<Quote>
{
    public void Configure(EntityTypeBuilder<Quote> builder)
    {
        builder.ToTable("quotes");
        builder.HasKey(quote => quote.Id);
        builder.Property(quote => quote.Id).ValueGeneratedNever();

        builder.Property(quote => quote.Version).IsRequired();
        builder.Property(quote => quote.Status).HasConversion<string>().HasMaxLength(15).IsUnicode(false).IsRequired();
        builder.Property(quote => quote.ServicesTotal).HasConversion<MoneyConverter>().HasPrecision(12, 2).IsRequired();
        builder.Property(quote => quote.PartsTotal).HasConversion<MoneyConverter>().HasPrecision(12, 2).IsRequired();
        builder.Property(quote => quote.Total).HasConversion<MoneyConverter>().HasPrecision(12, 2).IsRequired();
        builder.Property(quote => quote.ApprovalToken)
            .HasConversion<ApprovalTokenConverter>()
            .HasColumnName("tokenHash")
            .HasMaxLength(ApprovalToken.HashLength)
            .IsUnicode(false);
        builder.Property(quote => quote.SentAt).IsRequired();

        builder.HasIndex(quote => quote.ApprovalToken)
            .IsUnique()
            .HasFilter("[tokenHash] IS NOT NULL")
            .HasDatabaseName("ux_quotes_tokenHash");
        builder.HasIndex(quote => new { quote.ServiceOrderId, quote.Version })
            .IsUnique()
            .HasDatabaseName("ux_quotes_serviceOrderId_version");
    }
}
