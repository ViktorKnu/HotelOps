using HotelOps.Domene.Romadministrasjon;

namespace HotelOps.Applikasjon.Romadministrasjon;

public sealed class Romrengjøring(IRomskriver romskriver)
{
    public Task<RomDto?> PlanleggAsync(Guid romId, Guid forventetVersjon, string? ansvarligRenholder,
        Renholdsprioritet prioritet, CancellationToken avbryt = default) =>
        EndreStatusAsync(romId, forventetVersjon, rom => rom.PlanleggRenhold(ansvarligRenholder, prioritet), avbryt);

    public Task<RomDto?> MarkerSomSkittenAsync(
        Guid romId,
        Guid forventetVersjon,
        CancellationToken avbryt = default) =>
        EndreStatusAsync(romId, forventetVersjon, rom => rom.MarkerSomSkitten(), avbryt);

    public Task<RomDto?> StartAsync(
        Guid romId,
        Guid forventetVersjon,
        CancellationToken avbryt = default) =>
        EndreStatusAsync(romId, forventetVersjon, rom => rom.StartRengjøring(), avbryt);

    public Task<RomDto?> FullførAsync(
        Guid romId,
        Guid forventetVersjon,
        CancellationToken avbryt = default) =>
        EndreStatusAsync(romId, forventetVersjon, rom => rom.FullførRengjøring(), avbryt);

    private async Task<RomDto?> EndreStatusAsync(
        Guid romId,
        Guid forventetVersjon,
        Action<Rom> endreStatus,
        CancellationToken avbryt)
    {
        var rom = await romskriver.HentAsync(romId, avbryt);

        if (rom is null)
        {
            return null;
        }

        if (rom.Versjon != forventetVersjon)
            throw new UtdatertRomversjonException();

        endreStatus(rom);
        await romskriver.LagreEndringerAsync(avbryt);

        return RomDto.FraRom(rom);
    }
}
