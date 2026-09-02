using HotelOps.Domene.Romadministrasjon;

namespace HotelOps.Applikasjon.Romadministrasjon;

public interface IRomskriver
{
    Task<bool> PrøvLeggTilAsync(Rom rom, CancellationToken avbryt = default);
}
