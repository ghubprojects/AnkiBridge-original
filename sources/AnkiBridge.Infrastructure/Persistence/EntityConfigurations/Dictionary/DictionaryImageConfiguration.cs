using AnkiBridge.Domain.Aggregates.Dictionary;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AnkiBridge.Infrastructure.Persistence.EntityConfigurations.Dictionary;

public sealed class DictionaryImageConfiguration : IEntityTypeConfiguration<DictionaryImage>
{
    public void Configure(EntityTypeBuilder<DictionaryImage> builder)
    {
        // Table name
        builder.ToTable("DictionaryImage", "Dictionary");

        // Primary key
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .ValueGeneratedNever();

        // Foreign key
        builder.Property<Guid>("DictionaryEntryId")
            .IsRequired();

        // Content
        builder.Property(x => x.Url)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(x => x.Source)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(20);

        // Indexes
        builder.HasIndex("DictionaryEntryId");
    }
}
