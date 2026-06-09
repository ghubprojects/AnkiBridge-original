using AnkiBridge.Domain.Aggregates.Dictionary;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AnkiBridge.Infrastructure.Persistence.EntityConfigurations.Dictionary;

public sealed class DictionaryEntryConfiguration : IEntityTypeConfiguration<DictionaryEntry>
{
    public void Configure(EntityTypeBuilder<DictionaryEntry> builder)
    {
        // Table
        builder.ToTable("DictionaryEntry", "Dictionary");

        // Primary key
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .ValueGeneratedNever();

        // Core
        builder.Property(x => x.Headword)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(x => x.PartOfSpeech)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(x => x.Source)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(20);

        // Pronunciations
        builder.HasMany(x => x.Pronunciations)
            .WithOne()
            .HasForeignKey("DictionaryEntryId")
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(x => x.Pronunciations)
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        // Definitions
        builder.HasMany(x => x.Definitions)
            .WithOne()
            .HasForeignKey("DictionaryEntryId")
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(x => x.Definitions)
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        // Translations
        builder.HasMany(x => x.Translations)
            .WithOne()
            .HasForeignKey("DictionaryEntryId")
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(x => x.Translations)
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        // Images
        builder.HasMany(x => x.Images)
            .WithOne()
            .HasForeignKey("DictionaryEntryId")
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(x => x.Images)
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        // Indexes
        builder.HasIndex(x => x.Headword);
        builder.HasIndex(x => new { x.Headword, x.PartOfSpeech });

        // Domain events
        builder.Ignore(x => x.DomainEvents);
    }
}
