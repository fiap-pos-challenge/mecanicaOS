using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Oficina.Domain.Common.ValueObjects;
using Oficina.Domain.Customers.Entities;
using Oficina.Domain.Customers.ValueObjects;
using Oficina.Infrastructure.Persistence.Converters;

namespace Oficina.Infrastructure.Persistence.Configurations.Customers;

public sealed class CustomerConfiguration : IEntityTypeConfiguration<Customer>
{
    public void Configure(EntityTypeBuilder<Customer> builder)
    {
        builder.ToTable("customers");
        builder.HasKey(customer => customer.Id);
        builder.Property(customer => customer.Id).ValueGeneratedNever();
        builder.Ignore(customer => customer.DomainEvents);

        builder.Property(customer => customer.Name).HasMaxLength(Customer.NameMaxLength).IsRequired();
        builder.Property(customer => customer.Document)
            .HasConversion(document => document.Value, value => Document.Create(value).Value)
            .HasMaxLength(14)
            .IsUnicode(false)
            .IsRequired();
        builder.Property(customer => customer.DocumentType).HasConversion<string>().HasMaxLength(4).IsUnicode(false).IsRequired();
        builder.Property(customer => customer.Email).HasConversion<EmailConverter>().HasMaxLength(Email.MaxLength);
        builder.Property(customer => customer.Phone)
            .HasConversion(phone => phone.Value, value => Phone.Create(value).Value)
            .HasMaxLength(20)
            .IsUnicode(false)
            .IsRequired();

        builder.OwnsOne(customer => customer.Address, address =>
        {
            address.Property(value => value.ZipCode).HasColumnName("addressZipCode").HasColumnType("char(8)");
            address.Property(value => value.Street).HasColumnName("addressStreet").HasMaxLength(Address.StreetMaxLength);
            address.Property(value => value.Number).HasColumnName("addressNumber").HasMaxLength(Address.NumberMaxLength);
            address.Property(value => value.Complement).HasColumnName("addressComplement").HasMaxLength(Address.ComplementMaxLength);
            address.Property(value => value.District).HasColumnName("addressDistrict").HasMaxLength(Address.DistrictMaxLength);
            address.Property(value => value.City).HasColumnName("addressCity").HasMaxLength(Address.CityMaxLength);
            address.Property(value => value.State).HasColumnName("addressState").HasColumnType("char(2)");
        });
        builder.Navigation(customer => customer.Address).IsRequired(false);

        builder.HasIndex(customer => customer.Document).IsUnique().HasFilter("[deletedAt] IS NULL").HasDatabaseName("ux_customers_document");
    }
}
