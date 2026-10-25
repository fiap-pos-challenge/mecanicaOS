using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Oficina.Domain.ServiceOrders.Entities;

namespace Oficina.Infrastructure.Persistence.Configurations.ServiceOrders;

public sealed class ServiceOrderPhotoConfiguration : IEntityTypeConfiguration<ServiceOrderPhoto>
{
    public void Configure(EntityTypeBuilder<ServiceOrderPhoto> builder)
    {
        builder.ToTable("serviceOrderPhotos");
        builder.HasKey(photo => photo.Id);
        builder.Property(photo => photo.Id).ValueGeneratedNever();
        builder.Property(photo => photo.ClientId).HasMaxLength(ServiceOrderPhoto.ClientIdMaxLength).IsUnicode(false);
        builder.Property(photo => photo.ContentType).HasMaxLength(20).IsUnicode(false).IsRequired();
        builder.Property(photo => photo.DataUrl).IsUnicode(false).IsRequired();
        builder.Property(photo => photo.Caption).HasMaxLength(ServiceOrderPhoto.CaptionMaxLength).IsRequired();
        builder.Property(photo => photo.TakenAt).IsRequired();
        builder.Ignore(photo => photo.DomainEvents);
        builder.HasOne<ServiceOrder>().WithMany().HasForeignKey(photo => photo.ServiceOrderId).OnDelete(DeleteBehavior.ClientCascade);
    }
}
