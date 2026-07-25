using AnkiBridge.Domain.Aggregates.Learning;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AnkiBridge.Infrastructure.Persistence.EntityConfigurations.Learning;

public sealed class LearningEntryConfiguration : IEntityTypeConfiguration<LearningEntry>
{
    public void Configure(EntityTypeBuilder<LearningEntry> builder)
    {
        // Table
        builder.ToTable("LearningEntry", "Learning");

        // Primary key
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .ValueGeneratedNever();

        // Reference
        builder.Property(x => x.DictionaryEntryId)
            .IsRequired(false);

        // Core
        builder.Property(x => x.Headword)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(x => x.PartOfSpeech)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(x => x.Accent)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(10);

        builder.Property(x => x.Ipa)
            .IsRequired()
            .HasMaxLength(50);

        // Learning content
        builder.Property(x => x.Cloze)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(x => x.Definition)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(x => x.TranslationSource)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(x => x.Translation)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(x => x.AudioSource)
            .IsRequired(false)
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(x => x.AudioPath)
            .IsRequired(false)
            .HasMaxLength(500);

        builder.Property(x => x.AudioUploadStatus)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(x => x.AudioUploadError)
            .IsRequired(false)
            .HasMaxLength(500);

        builder.Property(x => x.ImageSource)
            .IsRequired(false)
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(x => x.ImagePath)
            .IsRequired(false)
            .HasMaxLength(500);

        builder.Property(x => x.ImageUploadStatus)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(x => x.ImageUploadError)
            .IsRequired(false)
            .HasMaxLength(500);

        // Examples
        builder.HasMany(x => x.Examples)
           .WithOne()
           .HasForeignKey("LearningEntryId")
           .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(x => x.Examples)
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        // Audit
        builder.Property(x => x.CreatedAt)
            .IsRequired();

        builder.Property(x => x.CreatedBy)
            .IsRequired();

        builder.Property(x => x.LastModifiedAt);
        builder.Property(x => x.LastModifiedBy);

        // Soft Delete
        builder.Property(x => x.IsDeleted)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(x => x.DeletedAt);
        builder.Property(x => x.DeletedBy);

        builder.HasQueryFilter(x => !x.IsDeleted);

        // Indexes
        builder.HasIndex(x => x.Headword);
        builder.HasIndex(x => new { x.Headword, x.PartOfSpeech });
        builder.HasIndex(x => x.CreatedAt);

        // Domain events
        builder.Ignore(x => x.DomainEvents);
    }
}
