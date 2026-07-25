using AnkiBridge.Domain.Aggregates.Dictionary;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AnkiBridge.Infrastructure.Persistence.EntityConfigurations.Dictionary;

public sealed class DictionaryPronunciationConfiguration : IEntityTypeConfiguration<DictionaryPronunciation>
{
    public void Configure(EntityTypeBuilder<DictionaryPronunciation> builder)
    {
        // Preserve the existing physical table name after the domain type rename.
        builder.ToTable("Pronunciation", "Dictionary");

        // Primary key
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .ValueGeneratedNever();

        // Foreign key
        builder.Property<Guid>("DictionaryEntryId")
            .IsRequired();

        // Content
        builder.Property(x => x.Ipa)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(x => x.Accent)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(10);

        builder.Property(x => x.AudioUrl)
            .IsRequired(false)
            .HasMaxLength(500);

        builder.Property(x => x.AudioSource)
            .IsRequired(false)
            .HasConversion<string>()
            .HasMaxLength(20);

        // Indexes
        builder.HasIndex("DictionaryEntryId");
        builder.HasIndex(x => new { x.Ipa, x.Accent });
    }
}
