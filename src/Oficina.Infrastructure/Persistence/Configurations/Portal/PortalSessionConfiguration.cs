using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Oficina.Domain.Portal.Entities;
using Oficina.Domain.Quotes.Entities;

namespace Oficina.Infrastructure.Persistence.Configurations.Portal;

public sealed class PortalSessionConfiguration : IEntityTypeConfiguration<PortalSession>
{
    public void Configure(EntityTypeBuilder<PortalSession> builder)
    {
        builder.ToTable("portalSessions");
        builder.HasKey(session => session.Id);
        builder.Property(session => session.Id).ValueGeneratedNever();
        builder.Property(session => session.TokenHash).HasMaxLength(64).IsFixedLength().IsUnicode(false).IsRequired();
        builder.Property(session => session.ExpiresAt).IsRequired();
        builder.HasOne<Estimate>().WithMany().HasForeignKey(session => session.EstimateId).OnDelete(DeleteBehavior.Cascade);
        builder.HasIndex(session => session.TokenHash).IsUnique().HasDatabaseName("ux_portalSessions_tokenHash");
    }
}
