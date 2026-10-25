using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Oficina.Domain.Identity.Entities;

namespace Oficina.Infrastructure.Persistence.Configurations.Identity;

public sealed class UserSessionConfiguration : IEntityTypeConfiguration<UserSession>
{
    public void Configure(EntityTypeBuilder<UserSession> builder)
    {
        builder.ToTable("userSessions");
        builder.HasKey(session => session.Id);
        builder.Property(session => session.Id).ValueGeneratedNever();
        builder.Property(session => session.TokenHash).HasMaxLength(UserSession.TokenHashLength).IsFixedLength().IsUnicode(false).IsRequired();
        builder.Property(session => session.Device).HasMaxLength(UserSession.DeviceMaxLength).IsRequired();
        builder.HasOne<User>().WithMany().HasForeignKey(session => session.UserId).OnDelete(DeleteBehavior.Cascade);
        builder.HasIndex(session => session.TokenHash).IsUnique().HasDatabaseName("ux_userSessions_tokenHash");
    }
}
