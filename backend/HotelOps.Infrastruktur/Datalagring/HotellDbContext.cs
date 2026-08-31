using HotelOps.Domene.Romadministrasjon;
using Microsoft.EntityFrameworkCore;

namespace HotelOps.Infrastruktur.Datalagring;

public sealed class HotellDbContext(DbContextOptions<HotellDbContext> valg) : DbContext(valg)
{
    public DbSet<Rom> Rom => Set<Rom>();

    protected override void OnModelCreating(ModelBuilder modellbygger)
    {
        base.OnModelCreating(modellbygger);
        modellbygger.ApplyConfiguration(new RomKonfigurasjon());
    }
}
