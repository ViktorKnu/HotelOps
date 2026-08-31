using HotelOps.Domene.Romadministrasjon;

namespace HotelOps.Applikasjon.Romadministrasjon;

public interface IRomleser
{
    Task<IReadOnlyList<Rom>> HentAlleAsync(CancellationToken avbryt = default);
}
