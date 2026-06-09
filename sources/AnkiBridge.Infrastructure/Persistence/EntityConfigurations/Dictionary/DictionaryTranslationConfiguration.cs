using AnkiBridge.Domain.Aggregates.Dictionary;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AnkiBridge.Infrastructure.Persistence.EntityConfigurations.Dictionary;

public sealed class DictionaryTranslationConfiguration : IEntityTypeConfiguration<DictionaryTranslation>
{
    public void Configure(EntityTypeBuilder<DictionaryTranslation> builder)
    {
        builder.ToTable("DictionaryTranslation", "Dictionary");

        builder.HasKey(x => x.Id);

        builder.Property<Guid>("DictionaryEntryId").IsRequired();

        builder.Property(x => x.Text)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(x => x.Source)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.HasIndex("DictionaryEntryId");
    }
}