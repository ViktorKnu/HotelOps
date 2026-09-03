using HotelOps.Domene.Romadministrasjon;

namespace HotelOps.Applikasjon.Romadministrasjon;

public interface IRomskriver
{
    Task<Rom?> HentAsync(Guid romId, CancellationToken avbryt = default);

    Task<bool> PrøvLeggTilAsync(Rom rom, CancellationToken avbryt = default);

    Task LagreEndringerAsync(CancellationToken avbryt = default);
}
