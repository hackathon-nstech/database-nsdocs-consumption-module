using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NSdocs.Domain.Entities;
using NSdocs.Domain.Enums;
using NSdocs.Infrastructure.Data.Converters;

namespace NSdocs.Infrastructure.Data.Configurations;

public class ConsumptionConfiguration : IEntityTypeConfiguration<Consumption>
{
    public void Configure(EntityTypeBuilder<Consumption> builder)
    {
        builder.ToTable("consumption");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.CompanyId)
            .HasColumnName("id_company")
            .IsRequired();

        builder.Property(c => c.ConsumptionDate)
            .HasColumnName("consumption_date")
            .IsRequired();

        builder.Property(c => c.Origin)
            .HasColumnName("origin")
            .HasConversion(new EnumToStringConverter<DocumentOrigin>())
            .IsRequired();

        builder.Property(c => c.DocumentType)
            .HasColumnName("document_type")
            .HasConversion(new EnumToStringConverter<DocumentType>())
            .IsRequired();

        builder.Property(c => c.Status)
            .HasColumnName("status")
            .HasConversion(new EnumToStringConverter<DocumentStatus>())
            .IsRequired();

        builder.Property(c => c.Quantity)
            .HasColumnName("quantity")
            .IsRequired();

        builder.Property(c => c.Total)
            .HasColumnName("total")
            .IsRequired();

        builder.HasIndex(c => new { c.CompanyId, c.ConsumptionDate, c.Origin, c.DocumentType, c.Status })
            .HasDatabaseName("uk_company_type_origin_date_status")
            .IsUnique();
    }
}
