using HotelOps.Applikasjon.Romadministrasjon;
using HotelOps.Domene.Romadministrasjon;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace HotelOps.Infrastruktur.Datalagring;

internal sealed class Romskriver(HotellDbContext database) : IRomskriver
{
    public async Task<Rom?> HentAsync(Guid romId, CancellationToken avbryt = default) =>
        await database.Rom.FindAsync([romId], avbryt);

    public async Task<bool> PrøvLeggTilAsync(Rom rom, CancellationToken avbryt = default)
    {
        database.Rom.Add(rom);

        try
        {
            await database.SaveChangesAsync(avbryt);
            return true;
        }
        catch (DbUpdateException feil) when (
            feil.InnerException is PostgresException
            {
                SqlState: PostgresErrorCodes.UniqueViolation,
                ConstraintName: "IX_Rom_Nummer"
            })
        {
            database.Entry(rom).State = EntityState.Detached;
            return false;
        }
    }

    public async Task LagreEndringerAsync(CancellationToken avbryt = default)
    {
        try
        {
            await database.SaveChangesAsync(avbryt);
        }
        catch (DbUpdateConcurrencyException feil)
        {
            throw new RomkonfliktException(feil);
        }
    }
}
