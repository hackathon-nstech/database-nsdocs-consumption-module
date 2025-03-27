using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NSdocs.Domain.Entities;
using NSdocs.Domain.Enums;
using NSdocs.Infrastructure.Data.Converters;

namespace NSdocs.Infrastructure.Data.Configurations;

public class DocumentConfiguration : IEntityTypeConfiguration<Document>
{
    public void Configure(EntityTypeBuilder<Document> builder)
    {
        builder.ToTable("documents");

        builder.HasKey(d => d.Id);

        builder.Property(d => d.CompanyId)
            .HasColumnName("id_company")
            .IsRequired();

        builder.Property(d => d.AccessKey)
            .HasColumnName("access_key")
            .HasMaxLength(44)
            .IsRequired();

        builder.Property(d => d.RequestDate)
            .HasColumnName("request_date")
            .IsRequired();

        builder.Property(d => d.UpdatedDate)
            .HasColumnName("updated_date")
            .IsRequired();

        builder.Property(d => d.Origin)
            .HasColumnName("origin")
            .HasConversion(new EnumToStringConverter<DocumentOrigin>())
            .IsRequired();

        builder.Property(d => d.DocumentType)
            .HasColumnName("document_type")
            .HasConversion(new EnumToStringConverter<DocumentType>())
            .IsRequired();

        builder.Property(d => d.Status)
            .HasColumnName("status")
            .HasConversion(new EnumToStringConverter<DocumentStatus>())
            .IsRequired();

        builder.HasIndex(d => new { d.AccessKey, d.CompanyId })
            .HasDatabaseName("uk_access_key_company")
            .IsUnique();
    }
}
