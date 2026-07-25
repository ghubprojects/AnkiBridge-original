using AnkiBridge.Domain.Aggregates.Dictionary;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AnkiBridge.Infrastructure.Persistence.EntityConfigurations.Dictionary;

public sealed class DictionaryDefinitionConfiguration : IEntityTypeConfiguration<DictionaryDefinition>
{
    public void Configure(EntityTypeBuilder<DictionaryDefinition> builder)
    {
        // Table name
        builder.ToTable("DictionaryDefinition", "Dictionary");

        // Primary key
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .ValueGeneratedNever();

        // Foreign key
        builder.Property<Guid>("DictionaryEntryId")
            .IsRequired();

        // Content
        builder.Property(x => x.Text)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(x => x.OrderIndex)
            .IsRequired();

        // Examples
        builder.HasMany(x => x.Examples)
            .WithOne()
            .HasForeignKey("DefinitionId")
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(x => x.Examples)
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        // Indexes
        builder.HasIndex("DictionaryEntryId");
    }
}
