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
        rom.Property(r => r.Versjon).IsConcurrencyToken().ValueGeneratedNever();
        rom.Property(r => r.Nummer).IsRequired();
        rom.HasIndex(r => r.Nummer).IsUnique();

        rom.Property(r => r.Beleggsstatus).HasConversion<string>().HasMaxLength(32);
        rom.Property(r => r.Rengjøringsstatus).HasConversion<string>().HasMaxLength(32);
        rom.Property(r => r.Driftsstatus).HasConversion<string>().HasMaxLength(32);
        rom.Property(r => r.AnsvarligRenholder).HasMaxLength(100);
        rom.Property(r => r.Renholdsprioritet).HasConversion<string>().HasMaxLength(32)
            .HasDefaultValue(Renholdsprioritet.Normal);

        // Lagring må fortsatt bygge på den tilstanden som ble lest i forespørselen.
        rom.Property(r => r.Beleggsstatus).IsConcurrencyToken();
        rom.Property(r => r.Rengjøringsstatus).IsConcurrencyToken();
        rom.Property(r => r.Driftsstatus).IsConcurrencyToken();
        rom.Property(r => r.AnsvarligRenholder).IsConcurrencyToken();
        rom.Property(r => r.Renholdsprioritet).IsConcurrencyToken();

        rom.Ignore(r => r.ErKlartForInnsjekking);
    }
}
