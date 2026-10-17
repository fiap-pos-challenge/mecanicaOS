using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Oficina.Domain.Identity.Entities;
using Oficina.Domain.Team.Entities;
using Oficina.Infrastructure.Persistence.Converters;

namespace Oficina.Infrastructure.Persistence.Configurations.Team;

public sealed class TeamProfileConfiguration : IEntityTypeConfiguration<TeamProfile>
{
    public void Configure(EntityTypeBuilder<TeamProfile> builder)
    {
        builder.ToTable("teamProfiles");
        builder.HasKey(profile => profile.UserId);
        builder.Property(profile => profile.UserId).ValueGeneratedNever();
        builder.PrimitiveCollection(profile => profile.Specialties).IsRequired();
        builder.Property(profile => profile.CommissionPercent).HasPrecision(5, 2).IsRequired();
        builder.Property(profile => profile.DiscountLimitPercent).HasPrecision(5, 2).IsRequired();
        builder.Property(profile => profile.Schedule).HasJsonConversion().IsRequired();
        builder.HasOne<User>().WithOne().HasForeignKey<TeamProfile>(profile => profile.UserId).OnDelete(DeleteBehavior.Cascade);
    }
}
