using HotelOps.Infrastruktur.Datalagring;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace HotelOps.Infrastruktur;

public static class Avhengighetsregistrering
{
    public static IServiceCollection LeggTilInfrastruktur(
        this IServiceCollection tjenester,
        IConfiguration konfigurasjon)
    {
        tjenester.AddDbContext<HotellDbContext>(valg =>
        {
            var tilkoblingsstreng = konfigurasjon.GetConnectionString("Hotell");

            if (string.IsNullOrWhiteSpace(tilkoblingsstreng))
            {
                throw new InvalidOperationException(
                    "Tilkoblingsstrengen 'ConnectionStrings:Hotell' må konfigureres.");
            }

            valg.UseNpgsql(tilkoblingsstreng);
        });

        return tjenester;
    }
}
