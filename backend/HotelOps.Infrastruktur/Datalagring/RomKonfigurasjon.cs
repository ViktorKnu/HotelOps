using HotelOps.Domene.Romadministrasjon;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HotelOps.Infrastruktur.Datalagring;

internal sealed class RomKonfigurasjon : IEntityTypeConfiguration<Rom>
{
    public void Configure(EntityTypeBuilder<Rom> rom)
    {
        rom.ToTable("Rom");
        rom.HasKey(r => r.Id);
        rom.Property(r => r.Id).ValueGeneratedNever();
        rom.Property(r => r.Nummer).IsRequired();
        rom.HasIndex(r => r.Nummer).IsUnique();

        rom.Property(r => r.Beleggsstatus).HasConversion<string>().HasMaxLength(32);
        rom.Property(r => r.Rengjøringsstatus).HasConversion<string>().HasMaxLength(32);
        rom.Property(r => r.Driftsstatus).HasConversion<string>().HasMaxLength(32);
        rom.Property(r => r.AnsvarligRenholder).HasMaxLength(100);
        rom.Property(r => r.Renholdsprioritet).HasConversion<string>().HasMaxLength(32)
            .HasDefaultValue(Renholdsprioritet.Normal);

        rom.Ignore(r => r.ErKlartForInnsjekking);
    }
}
