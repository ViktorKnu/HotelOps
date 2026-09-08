using HotelOps.Applikasjon.Romadministrasjon;
using HotelOps.Infrastruktur;
using HotelOps.Infrastruktur.Datalagring;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace HotelOps.ApiTester;

public sealed class RomlagringskonfliktTester
{
    [Fact]
    public async Task DatabasekonfliktOversettesTilApplikasjonsfeilUtenAutomatiskNyttForsøk()
    {
        var avskjærer = new Konfliktavskjærer();
        var konfigurasjon = new ConfigurationBuilder().AddInMemoryCollection(
            new Dictionary<string, string?> { ["ConnectionStrings:Hotell"] = "Host=localhost;Database=ikke_brukt" }).Build();
        var tjenester = new ServiceCollection();
        tjenester.LeggTilInfrastruktur(konfigurasjon);
        tjenester.AddDbContext<HotellDbContext>(valg => valg.AddInterceptors(avskjærer));
        await using var tilbyder = tjenester.BuildServiceProvider();
        await using var scope = tilbyder.CreateAsyncScope();
        var skriver = scope.ServiceProvider.GetRequiredService<IRomskriver>();

        var feil = await Assert.ThrowsAsync<RomkonfliktException>(() => skriver.LagreEndringerAsync());

        Assert.IsType<DbUpdateConcurrencyException>(feil.InnerException);
        Assert.Equal(1, avskjærer.AntallForsøk);
    }

    private sealed class Konfliktavskjærer : SaveChangesInterceptor
    {
        public int AntallForsøk { get; private set; }

        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
            DbContextEventData eventData, InterceptionResult<int> result,
            CancellationToken cancellationToken = default)
        {
            AntallForsøk++;
            throw new DbUpdateConcurrencyException("Simulert samtidig databaseendring.");
        }
    }
}
