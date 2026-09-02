using HotelOps.Domene.Romadministrasjon;

namespace HotelOps.Applikasjon.Romadministrasjon;

public sealed class Romregistrering(IRomskriver romskriver)
{
    public async Task<RomDto?> PrøvOpprettAsync(
        string nummer,
        int etasje,
        CancellationToken avbryt = default)
    {
        var rom = new Rom(nummer, etasje);

        if (!await romskriver.PrøvLeggTilAsync(rom, avbryt))
        {
            return null;
        }

        return RomDto.FraRom(rom);
    }
}
