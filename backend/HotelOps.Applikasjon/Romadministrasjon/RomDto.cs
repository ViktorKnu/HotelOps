using HotelOps.Domene.Romadministrasjon;

namespace HotelOps.Applikasjon.Romadministrasjon;

public sealed record RomDto(
    Guid Id,
    string Nummer,
    int Etasje,
    Beleggsstatus Beleggsstatus,
    Rengjøringsstatus Rengjøringsstatus,
    Driftsstatus Driftsstatus,
    bool ErKlartForInnsjekking)
{
    public static RomDto FraRom(Rom rom) => new(
        rom.Id,
        rom.Nummer,
        rom.Etasje,
        rom.Beleggsstatus,
        rom.Rengjøringsstatus,
        rom.Driftsstatus,
        rom.ErKlartForInnsjekking);
}
