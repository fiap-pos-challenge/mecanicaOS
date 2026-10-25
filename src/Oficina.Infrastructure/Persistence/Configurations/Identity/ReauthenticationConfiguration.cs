using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Oficina.Domain.Identity.Entities;

namespace Oficina.Infrastructure.Persistence.Configurations.Identity;

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
