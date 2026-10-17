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

public sealed class TwoFactorChallengeConfiguration : IEntityTypeConfiguration<TwoFactorChallenge>
{
    public void Configure(EntityTypeBuilder<TwoFactorChallenge> builder)
    {
        builder.ToTable("twoFactorChallenges");
        builder.HasKey(challenge => challenge.Id);
        builder.Property(challenge => challenge.Id).ValueGeneratedNever();
        builder.HasOne<User>().WithMany().HasForeignKey(challenge => challenge.UserId).OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class ReauthenticationConfiguration : IEntityTypeConfiguration<Reauthentication>
{
    public void Configure(EntityTypeBuilder<Reauthentication> builder)
    {
        builder.ToTable("reauthentications");
        builder.HasKey(reauthentication => reauthentication.Id);
        builder.Property(reauthentication => reauthentication.Id).ValueGeneratedNever();
        builder.Property(reauthentication => reauthentication.TokenHash).HasMaxLength(Reauthentication.TokenHashLength).IsFixedLength().IsUnicode(false).IsRequired();
        builder.HasOne<User>().WithMany().HasForeignKey(reauthentication => reauthentication.UserId).OnDelete(DeleteBehavior.Cascade);
        builder.HasIndex(reauthentication => reauthentication.TokenHash).IsUnique().HasDatabaseName("ux_reauthentications_tokenHash");
    }
}
