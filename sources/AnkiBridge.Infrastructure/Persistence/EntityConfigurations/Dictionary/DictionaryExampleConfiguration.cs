using AnkiBridge.Domain.Aggregates.Dictionary;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AnkiBridge.Infrastructure.Persistence.EntityConfigurations.Dictionary;

public sealed class DictionaryExampleConfiguration : IEntityTypeConfiguration<DictionaryExample>
{
    public void Configure(EntityTypeBuilder<DictionaryExample> builder)
    {
        // Table name
        builder.ToTable("DictionaryExample", "Dictionary");

        // Primary key
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .ValueGeneratedNever();

        // Foreign key
        builder.Property<Guid>("DefinitionId")
            .IsRequired();

        // Content
        builder.Property(x => x.Text)
            .IsRequired()
            .HasMaxLength(200);

        // Indexes
        builder.HasIndex("DefinitionId");
    }
}
