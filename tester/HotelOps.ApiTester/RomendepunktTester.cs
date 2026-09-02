using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using HotelOps.Applikasjon.Romadministrasjon;
using HotelOps.Domene.Romadministrasjon;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;

namespace HotelOps.ApiTester;

public sealed class RomendepunktTester
{
    [Fact]
    public async Task TomRomoversiktGir200OgTomListe()
    {
        await using var fabrikk = new RomApiFabrikk(new TestRomleser([]));
        using var klient = fabrikk.CreateClient();

        using var svar = await klient.GetAsync("/api/rom");

        Assert.Equal(HttpStatusCode.OK, svar.StatusCode);
        Assert.Equal("application/json", svar.Content.Headers.ContentType?.MediaType);
        Assert.Equal("[]", await svar.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task RomoversiktGirRomdataLesbareStatuserOgBeregnetKlarstatus()
    {
        var skittentRom = new Rom("101", 1, rengjøringsstatus: Rengjøringsstatus.Skitten);
        var klartRom = new Rom("102", 1);
        await using var fabrikk = new RomApiFabrikk(new TestRomleser([skittentRom, klartRom]));
        using var klient = fabrikk.CreateClient();

        using var svar = await klient.GetAsync("/api/rom");

        Assert.Equal(HttpStatusCode.OK, svar.StatusCode);
        using var dokument = JsonDocument.Parse(await svar.Content.ReadAsStringAsync());
        var rom = dokument.RootElement;
        Assert.Equal(2, rom.GetArrayLength());
        Assert.Equal(skittentRom.Id, rom[0].GetProperty("id").GetGuid());
        Assert.Equal("101", rom[0].GetProperty("nummer").GetString());
        Assert.Equal(1, rom[0].GetProperty("etasje").GetInt32());
        Assert.Equal("Ledig", rom[0].GetProperty("beleggsstatus").GetString());
        Assert.Equal("Skitten", rom[0].GetProperty("rengjøringsstatus").GetString());
        Assert.Equal("Operativ", rom[0].GetProperty("driftsstatus").GetString());
        Assert.False(rom[0].GetProperty("erKlartForInnsjekking").GetBoolean());
        Assert.Equal(klartRom.Id, rom[1].GetProperty("id").GetGuid());
        Assert.True(rom[1].GetProperty("erKlartForInnsjekking").GetBoolean());
    }

    [Fact]
    public async Task GyldigRomGir201OgStandardstatuser()
    {
        var romskriver = new TestRomskriver();
        await using var fabrikk = new RomApiFabrikk(
            new TestRomleser([]),
            romskriver: romskriver);
        using var klient = fabrikk.CreateClient();

        using var svar = await klient.PostAsJsonAsync("/api/rom", new
        {
            nummer = " 103 ",
            etasje = 1
        });

        Assert.Equal(HttpStatusCode.Created, svar.StatusCode);
        Assert.Equal("/api/rom", svar.Headers.Location?.ToString());
        Assert.NotNull(romskriver.SistLagtTil);
        Assert.Equal("103", romskriver.SistLagtTil.Nummer);
        using var dokument = JsonDocument.Parse(await svar.Content.ReadAsStringAsync());
        var rom = dokument.RootElement;
        Assert.Equal(romskriver.SistLagtTil.Id, rom.GetProperty("id").GetGuid());
        Assert.Equal("Ledig", rom.GetProperty("beleggsstatus").GetString());
        Assert.Equal("Ren", rom.GetProperty("rengjøringsstatus").GetString());
        Assert.Equal("Operativ", rom.GetProperty("driftsstatus").GetString());
        Assert.True(rom.GetProperty("erKlartForInnsjekking").GetBoolean());
    }

    [Fact]
    public async Task TomtRomnummerGir400UtenÅLagre()
    {
        var romskriver = new TestRomskriver();
        await using var fabrikk = new RomApiFabrikk(
            new TestRomleser([]),
            romskriver: romskriver);
        using var klient = fabrikk.CreateClient();

        using var svar = await klient.PostAsJsonAsync("/api/rom", new
        {
            nummer = " ",
            etasje = 1
        });

        Assert.Equal(HttpStatusCode.BadRequest, svar.StatusCode);
        Assert.Equal("application/problem+json", svar.Content.Headers.ContentType?.MediaType);
        Assert.Equal(0, romskriver.AntallKall);
        using var dokument = JsonDocument.Parse(await svar.Content.ReadAsStringAsync());
        var feil = dokument.RootElement.GetProperty("errors").GetProperty("nummer")[0];
        Assert.Equal("Romnummer må oppgis.", feil.GetString());
    }

    [Fact]
    public async Task EksisterendeRomnummerGir409()
    {
        var romskriver = new TestRomskriver(kanLagre: false);
        await using var fabrikk = new RomApiFabrikk(
            new TestRomleser([]),
            romskriver: romskriver);
        using var klient = fabrikk.CreateClient();

        using var svar = await klient.PostAsJsonAsync("/api/rom", new
        {
            nummer = "101",
            etasje = 1
        });

        Assert.Equal(HttpStatusCode.Conflict, svar.StatusCode);
        Assert.Equal("application/problem+json", svar.Content.Headers.ContentType?.MediaType);
        var innhold = await svar.Content.ReadAsStringAsync();
        Assert.Contains("Romnummeret er allerede i bruk.", innhold);
        Assert.Contains("Rom 101 er allerede registrert.", innhold);
    }

    [Theory]
    [InlineData("Development")]
    [InlineData("Production")]
    public async Task LesefeilGirProblemDetailsUtenInterneFeildetaljer(string miljø)
    {
        await using var fabrikk = new RomApiFabrikk(new FeilendeRomleser(), miljø);
        using var klient = fabrikk.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost"),
            AllowAutoRedirect = false
        });

        using var svar = await klient.GetAsync("/api/rom");

        Assert.Equal(HttpStatusCode.InternalServerError, svar.StatusCode);
        Assert.Equal("application/problem+json", svar.Content.Headers.ContentType?.MediaType);
        var innhold = await svar.Content.ReadAsStringAsync();
        using var dokument = JsonDocument.Parse(innhold);
        Assert.Equal(500, dokument.RootElement.GetProperty("status").GetInt32());
        Assert.Equal("En uventet feil oppstod.", dokument.RootElement.GetProperty("title").GetString());
        Assert.Equal("Prøv igjen senere.", dokument.RootElement.GetProperty("detail").GetString());
        Assert.DoesNotContain("intern-testdetalj", innhold);
        Assert.DoesNotContain(nameof(InvalidOperationException), innhold);
    }

    private sealed class RomApiFabrikk(
        IRomleser romleser,
        string miljø = "Development",
        IRomskriver? romskriver = null)
        : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder bygger)
        {
            bygger.UseEnvironment(miljø);
            bygger.ConfigureLogging(loggføring => loggføring.ClearProviders());
            bygger.ConfigureServices(tjenester =>
            {
                tjenester.RemoveAll<IRomleser>();
                tjenester.AddSingleton(romleser);

                if (romskriver is not null)
                {
                    tjenester.RemoveAll<IRomskriver>();
                    tjenester.AddSingleton(romskriver);
                }
            });
        }
    }

    private sealed class TestRomleser(IReadOnlyList<Rom> rom) : IRomleser
    {
        public Task<IReadOnlyList<Rom>> HentAlleAsync(CancellationToken avbryt = default) =>
            Task.FromResult(rom);
    }

    private sealed class FeilendeRomleser : IRomleser
    {
        public Task<IReadOnlyList<Rom>> HentAlleAsync(CancellationToken avbryt = default) =>
            throw new InvalidOperationException("intern-testdetalj");
    }

    private sealed class TestRomskriver(bool kanLagre = true) : IRomskriver
    {
        public Rom? SistLagtTil { get; private set; }

        public int AntallKall { get; private set; }

        public Task<bool> PrøvLeggTilAsync(Rom rom, CancellationToken avbryt = default)
        {
            AntallKall++;
            SistLagtTil = rom;
            return Task.FromResult(kanLagre);
        }
    }
}
