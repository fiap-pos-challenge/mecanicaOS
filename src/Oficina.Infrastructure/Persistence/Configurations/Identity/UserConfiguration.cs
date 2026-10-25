using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Oficina.Domain.Common.ValueObjects;
using Oficina.Domain.Identity.Entities;
using Oficina.Infrastructure.Persistence.Converters;

namespace Oficina.Infrastructure.Persistence.Configurations.Identity;

public sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("users");
        builder.HasKey(user => user.Id);
        builder.Property(user => user.Id).ValueGeneratedNever();
        builder.Ignore(user => user.DomainEvents);
        builder.Ignore(user => user.Status);

        builder.Property(user => user.Name).HasMaxLength(User.NameMaxLength).IsRequired();
        builder.Property(user => user.Email).HasConversion<EmailConverter>().HasMaxLength(Email.MaxLength).IsRequired();
        builder.Property(user => user.PasswordHash).HasMaxLength(User.PasswordHashMaxLength);
        builder.Property(user => user.Role).HasConversion<string>().HasMaxLength(20).IsUnicode(false).IsRequired();
        builder.Property(user => user.IsActive).IsRequired();
        builder.Property(user => user.FailedLoginAttempts).IsRequired();
        builder.Property(user => user.LockoutEndAt);

        builder.Property(user => user.TwoFactorSecret).HasMaxLength(User.TwoFactorSecretMaxLength).IsUnicode(false);
        builder.Property(user => user.TwoFactorEnabled).IsRequired();
        builder.HasIndex(user => user.Email).IsUnique().HasFilter("[deletedAt] IS NULL").HasDatabaseName("ux_users_email");
    }
}
