using HotelOps.Domene.Romadministrasjon;

namespace HotelOps.Applikasjon.Romadministrasjon;

public sealed class Romrengjøring(IRomskriver romskriver)
{
    public Task<RomDto?> MarkerSomSkittenAsync(
        Guid romId,
        CancellationToken avbryt = default) =>
        EndreStatusAsync(romId, rom => rom.MarkerSomSkitten(), avbryt);

    public Task<RomDto?> StartAsync(
        Guid romId,
        CancellationToken avbryt = default) =>
        EndreStatusAsync(romId, rom => rom.StartRengjøring(), avbryt);

    public Task<RomDto?> FullførAsync(
        Guid romId,
        CancellationToken avbryt = default) =>
        EndreStatusAsync(romId, rom => rom.FullførRengjøring(), avbryt);

    private async Task<RomDto?> EndreStatusAsync(
        Guid romId,
        Action<Rom> endreStatus,
        CancellationToken avbryt)
    {
        var rom = await romskriver.HentAsync(romId, avbryt);

        if (rom is null)
        {
            return null;
        }

        endreStatus(rom);
        await romskriver.LagreEndringerAsync(avbryt);

        return RomDto.FraRom(rom);
    }
}
