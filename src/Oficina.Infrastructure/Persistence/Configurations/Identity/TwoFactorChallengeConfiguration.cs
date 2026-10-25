using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Oficina.Domain.Identity.Entities;

namespace Oficina.Infrastructure.Persistence.Configurations.Identity;

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
