using AnkiBridge.Domain.Aggregates.Learning;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AnkiBridge.Infrastructure.Persistence.EntityConfigurations.Learning;

public sealed class LearningExampleConfiguration : IEntityTypeConfiguration<LearningExample>
{
    public void Configure(EntityTypeBuilder<LearningExample> builder)
    {
        // Table name
        builder.ToTable("LearningExample", "Learning");

        // Primary key
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .ValueGeneratedNever();

        // Foreign key
        builder.Property<Guid>("LearningEntryId")
            .IsRequired();

        // Content
        builder.Property(x => x.Text)
            .IsRequired()
            .HasMaxLength(200);

        // Indexes
        builder.HasIndex("LearningEntryId");
    }
}
