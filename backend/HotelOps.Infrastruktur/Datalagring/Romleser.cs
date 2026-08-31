using HotelOps.Applikasjon.Romadministrasjon;
using HotelOps.Domene.Romadministrasjon;
using Microsoft.EntityFrameworkCore;

namespace HotelOps.Infrastruktur.Datalagring;

internal sealed class Romleser(HotellDbContext database) : IRomleser
{
    public async Task<IReadOnlyList<Rom>> HentAlleAsync(CancellationToken avbryt = default) =>
        await database.Rom
            .AsNoTracking()
            .OrderBy(rom => rom.Nummer)
            .ToListAsync(avbryt);
}
