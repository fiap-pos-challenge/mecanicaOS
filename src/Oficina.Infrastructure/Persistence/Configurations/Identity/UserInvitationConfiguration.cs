using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Oficina.Domain.Identity.Entities;

namespace Oficina.Infrastructure.Persistence.Configurations.Identity;

public sealed class UserInvitationConfiguration : IEntityTypeConfiguration<UserInvitation>
{
    public void Configure(EntityTypeBuilder<UserInvitation> builder)
    {
        builder.ToTable("userInvitations");
        builder.HasKey(invitation => invitation.Id);
        builder.Property(invitation => invitation.Id).ValueGeneratedNever();
        builder.Property(invitation => invitation.UserId).IsRequired();
        builder.Property(invitation => invitation.TokenHash).HasMaxLength(UserInvitation.TokenHashLength).IsFixedLength().IsUnicode(false).IsRequired();
        builder.Property(invitation => invitation.CreatedBy);
        builder.Property(invitation => invitation.CreatedAt).IsRequired();
        builder.Property(invitation => invitation.ExpiresAt).IsRequired();
        builder.Property(invitation => invitation.AcceptedAt);
        builder.Property(invitation => invitation.RevokedAt);

        builder.HasOne<User>().WithMany().HasForeignKey(invitation => invitation.UserId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(invitation => invitation.TokenHash).IsUnique().HasDatabaseName("ux_userInvitations_tokenHash");
        builder.HasIndex(invitation => invitation.UserId).HasDatabaseName("ix_userInvitations_user");
    }
}
