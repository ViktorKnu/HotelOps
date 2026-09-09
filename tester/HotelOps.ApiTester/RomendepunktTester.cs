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
    private static void SettRomversjon(HttpClient klient, Guid versjon)
    {
        klient.DefaultRequestHeaders.Remove("X-Rom-Versjon");
        klient.DefaultRequestHeaders.Add("X-Rom-Versjon", versjon.ToString());
    }

    [Theory]
    [InlineData("marker-skitten", Rengjøringsstatus.Ren)]
    [InlineData("start", Rengjøringsstatus.Skitten)]
    [InlineData("fullfor", Rengjøringsstatus.UnderRengjøring)]
    [InlineData("planlegg", Rengjøringsstatus.Skitten)]
    public async Task GammelVersjonAvvisesFørRommetEndres(string handling, Rengjøringsstatus status)
    {
        var rom = new Rom("101", 1, rengjøringsstatus: status);
        var versjon = rom.Versjon;
        var skriver = new TestRomskriver(rom: rom);
        await using var fabrikk = new RomApiFabrikk(new TestRomleser([rom]), romskriver: skriver);
        using var klient = fabrikk.CreateClient();
        SettRomversjon(klient, Guid.NewGuid());
        using var innhold = handling == "planlegg"
            ? JsonContent.Create(new { ansvarligRenholder = "Kari", prioritet = "Haster" }) : null;
        using var svar = await klient.PatchAsync($"/api/rom/{rom.Id}/rengjoring/{handling}", innhold);
        Assert.Equal(HttpStatusCode.Conflict, svar.StatusCode);
        Assert.Equal(status, rom.Rengjøringsstatus);
        Assert.Null(rom.AnsvarligRenholder);
        Assert.Equal(versjon, rom.Versjon);
        Assert.Equal(0, skriver.AntallLagringer);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("ugyldig")]
    public async Task ManglendeEllerUgyldigVersjonGir400(string? versjon)
    {
        var rom = new Rom("101", 1);
        var skriver = new TestRomskriver(rom: rom);
        await using var fabrikk = new RomApiFabrikk(new TestRomleser([rom]), romskriver: skriver);
        using var klient = fabrikk.CreateClient();
        if (versjon is not null) klient.DefaultRequestHeaders.Add("X-Rom-Versjon", versjon);
        using var svar = await klient.PatchAsync($"/api/rom/{rom.Id}/rengjoring/marker-skitten", null);
        Assert.Equal(HttpStatusCode.BadRequest, svar.StatusCode);
        Assert.Equal(0, skriver.AntallLagringer);
        Assert.Equal(Rengjøringsstatus.Ren, rom.Rengjøringsstatus);
    }

    [Fact]
    public async Task NyVersjonFraSvarKrevesForNesteEndring()
    {
        var rom = new Rom("101", 1, rengjøringsstatus: Rengjøringsstatus.Skitten);
        var skriver = new TestRomskriver(rom: rom);
        await using var fabrikk = new RomApiFabrikk(new TestRomleser([rom]), romskriver: skriver);
        using var klient = fabrikk.CreateClient();
        using var oversikt = JsonDocument.Parse(await klient.GetStringAsync("/api/rom"));
        var gammelVersjon = oversikt.RootElement[0].GetProperty("versjon").GetGuid();
        SettRomversjon(klient, gammelVersjon);
        using var førsteSvar = await klient.PatchAsJsonAsync($"/api/rom/{rom.Id}/rengjoring/planlegg",
            new { ansvarligRenholder = "Kari", prioritet = "Haster" });
        Assert.Equal(HttpStatusCode.OK, førsteSvar.StatusCode);
        using var oppdatert = JsonDocument.Parse(await førsteSvar.Content.ReadAsStringAsync());
        var nyVersjon = oppdatert.RootElement.GetProperty("versjon").GetGuid();
        Assert.NotEqual(gammelVersjon, nyVersjon);
        using var gammeltSvar = await klient.PatchAsJsonAsync($"/api/rom/{rom.Id}/rengjoring/planlegg",
            new { ansvarligRenholder = "Ola", prioritet = "Normal" });
        Assert.Equal(HttpStatusCode.Conflict, gammeltSvar.StatusCode);
        Assert.Equal("Kari", rom.AnsvarligRenholder);
        Assert.Equal(1, skriver.AntallLagringer);
        SettRomversjon(klient, nyVersjon);
        using var nyttSvar = await klient.PatchAsync($"/api/rom/{rom.Id}/rengjoring/start", null);
        Assert.Equal(HttpStatusCode.OK, nyttSvar.StatusCode);
        Assert.Equal(2, skriver.AntallLagringer);
    }

    [Theory]
    [InlineData("marker-skitten", Rengjøringsstatus.Ren)]
    [InlineData("start", Rengjøringsstatus.Skitten)]
    [InlineData("fullfor", Rengjøringsstatus.UnderRengjøring)]
    [InlineData("planlegg", Rengjøringsstatus.Skitten)]
    public async Task SamtidigLagringGir409MedVeiledning(string handling, Rengjøringsstatus status)
    {
        var rom = new Rom("101", 1, rengjøringsstatus: status);
        var skriver = new TestRomskriver(rom: rom, lagringskonflikt: true);
        await using var fabrikk = new RomApiFabrikk(new TestRomleser([rom]), romskriver: skriver);
        using var klient = fabrikk.CreateClient();
        using var innhold = handling == "planlegg"
            ? JsonContent.Create(new { ansvarligRenholder = "Kari", prioritet = "Haster" }) : null;
        SettRomversjon(klient, rom.Versjon);
        using var svar = await klient.PatchAsync($"/api/rom/{rom.Id}/rengjoring/{handling}", innhold);
        Assert.Equal(HttpStatusCode.Conflict, svar.StatusCode);
        Assert.Equal("application/problem+json", svar.Content.Headers.ContentType?.MediaType);
        using var dokument = JsonDocument.Parse(await svar.Content.ReadAsStringAsync());
        Assert.Equal("Rommet er endret av en annen bruker.", dokument.RootElement.GetProperty("title").GetString());
        Assert.Contains("Oppdater oversikten og prøv igjen", dokument.RootElement.GetProperty("detail").GetString());
        Assert.DoesNotContain("intern-testdetalj", await svar.Content.ReadAsStringAsync());
        Assert.Equal(0, skriver.AntallLagringer);
    }

    [Fact]
    public async Task RenholdsplanLagresOgReturneresIOversikten()
    {
        var rom = new Rom("101", 1, rengjøringsstatus: Rengjøringsstatus.Skitten);
        var skriver = new TestRomskriver(rom: rom);
        await using var fabrikk = new RomApiFabrikk(new TestRomleser([rom]), romskriver: skriver);
        using var klient = fabrikk.CreateClient();
        SettRomversjon(klient, rom.Versjon);
        using var svar = await klient.PatchAsJsonAsync($"/api/rom/{rom.Id}/rengjoring/planlegg",
            new { ansvarligRenholder = " Kari ", prioritet = "Haster" });
        Assert.Equal(HttpStatusCode.OK, svar.StatusCode);
        Assert.Equal(1, skriver.AntallLagringer);
        using var dokument = JsonDocument.Parse(await svar.Content.ReadAsStringAsync());
        Assert.Equal("Kari", dokument.RootElement.GetProperty("ansvarligRenholder").GetString());
        Assert.Equal("Haster", dokument.RootElement.GetProperty("renholdsprioritet").GetString());
        using var oversikt = await klient.GetAsync("/api/rom");
        using var liste = JsonDocument.Parse(await oversikt.Content.ReadAsStringAsync());
        Assert.Equal("Kari", liste.RootElement[0].GetProperty("ansvarligRenholder").GetString());
    }

    [Theory]
    [InlineData(null)]
    [InlineData("Ukjent")]
    [InlineData("1")]
    public async Task UgyldigPrioritetGir400UtenLagring(string? prioritet)
    {
        var rom = new Rom("101", 1, rengjøringsstatus: Rengjøringsstatus.Skitten);
        var skriver = new TestRomskriver(rom: rom);
        await using var fabrikk = new RomApiFabrikk(new TestRomleser([rom]), romskriver: skriver);
        using var klient = fabrikk.CreateClient();
        SettRomversjon(klient, rom.Versjon);
        using var svar = await klient.PatchAsJsonAsync($"/api/rom/{rom.Id}/rengjoring/planlegg",
            new { ansvarligRenholder = "Kari", prioritet });
        Assert.Equal(HttpStatusCode.BadRequest, svar.StatusCode);
        Assert.Equal(0, skriver.AntallLagringer);
    }

    [Fact]
    public async Task PlanleggingAvviserLangtNavnRentRomOgUkjentRom()
    {
        var rom = new Rom("101", 1);
        var skriver = new TestRomskriver(rom: rom);
        await using var fabrikk = new RomApiFabrikk(new TestRomleser([rom]), romskriver: skriver);
        using var klient = fabrikk.CreateClient();
        SettRomversjon(klient, rom.Versjon);
        using var langtNavn = await klient.PatchAsJsonAsync($"/api/rom/{rom.Id}/rengjoring/planlegg",
            new { ansvarligRenholder = new string('a', 101), prioritet = "Normal" });
        Assert.Equal(HttpStatusCode.BadRequest, langtNavn.StatusCode);
        SettRomversjon(klient, rom.Versjon);
        using var rentRom = await klient.PatchAsJsonAsync($"/api/rom/{rom.Id}/rengjoring/planlegg",
            new { ansvarligRenholder = "Kari", prioritet = "Normal" });
        Assert.Equal(HttpStatusCode.Conflict, rentRom.StatusCode);
        SettRomversjon(klient, rom.Versjon);
        using var ukjent = await klient.PatchAsJsonAsync($"/api/rom/{Guid.NewGuid()}/rengjoring/planlegg",
            new { ansvarligRenholder = "Kari", prioritet = "Normal" });
        Assert.Equal(HttpStatusCode.NotFound, ukjent.StatusCode);
        Assert.Equal(0, skriver.AntallLagringer);
    }

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

    [Fact]
    public async Task RengjøringsendepunkterFølgerHeleStatusflyten()
    {
        var rom = new Rom("101", 1);
        var romskriver = new TestRomskriver(rom: rom);
        await using var fabrikk = new RomApiFabrikk(
            new TestRomleser([rom]),
            romskriver: romskriver);
        using var klient = fabrikk.CreateClient();

        SettRomversjon(klient, rom.Versjon);
        using var skittenSvar = await klient.PatchAsync(
            $"/api/rom/{rom.Id}/rengjoring/marker-skitten",
            null);
        Assert.Equal(HttpStatusCode.OK, skittenSvar.StatusCode);
        Assert.Equal(Rengjøringsstatus.Skitten, rom.Rengjøringsstatus);
        await KontrollerRengjøringsstatusAsync(skittenSvar, "Skitten", erKlart: false);

        SettRomversjon(klient, rom.Versjon);
        using var startetSvar = await klient.PatchAsync(
            $"/api/rom/{rom.Id}/rengjoring/start",
            null);
        Assert.Equal(HttpStatusCode.OK, startetSvar.StatusCode);
        Assert.Equal(Rengjøringsstatus.UnderRengjøring, rom.Rengjøringsstatus);
        await KontrollerRengjøringsstatusAsync(startetSvar, "UnderRengjøring", erKlart: false);

        SettRomversjon(klient, rom.Versjon);
        using var fullførtSvar = await klient.PatchAsync(
            $"/api/rom/{rom.Id}/rengjoring/fullfor",
            null);
        Assert.Equal(HttpStatusCode.OK, fullførtSvar.StatusCode);
        Assert.Equal(Rengjøringsstatus.Ren, rom.Rengjøringsstatus);
        await KontrollerRengjøringsstatusAsync(fullførtSvar, "Ren", erKlart: true);

        Assert.Equal(3, romskriver.AntallLagringer);
    }

    [Fact]
    public async Task UgyldigRengjøringsovergangGir409UtenÅLagre()
    {
        var rom = new Rom("101", 1);
        var romskriver = new TestRomskriver(rom: rom);
        await using var fabrikk = new RomApiFabrikk(
            new TestRomleser([rom]),
            romskriver: romskriver);
        using var klient = fabrikk.CreateClient();

        SettRomversjon(klient, rom.Versjon);
        using var svar = await klient.PatchAsync(
            $"/api/rom/{rom.Id}/rengjoring/start",
            null);

        Assert.Equal(HttpStatusCode.Conflict, svar.StatusCode);
        Assert.Equal("application/problem+json", svar.Content.Headers.ContentType?.MediaType);
        var innhold = await svar.Content.ReadAsStringAsync();
        Assert.Contains("Rengjøringsstatusen kan ikke endres.", innhold);
        Assert.Contains("Rengjøring kan bare startes for et skittent rom.", innhold);
        Assert.Equal(Rengjøringsstatus.Ren, rom.Rengjøringsstatus);
        Assert.Equal(0, romskriver.AntallLagringer);
    }

    [Fact]
    public async Task RengjøringsendringForUkjentRomGir404()
    {
        var romskriver = new TestRomskriver();
        await using var fabrikk = new RomApiFabrikk(
            new TestRomleser([]),
            romskriver: romskriver);
        using var klient = fabrikk.CreateClient();
        var ukjentRomId = Guid.NewGuid();

        SettRomversjon(klient, Guid.NewGuid());
        using var svar = await klient.PatchAsync(
            $"/api/rom/{ukjentRomId}/rengjoring/marker-skitten",
            null);

        Assert.Equal(HttpStatusCode.NotFound, svar.StatusCode);
        Assert.Equal("application/problem+json", svar.Content.Headers.ContentType?.MediaType);
        var innhold = await svar.Content.ReadAsStringAsync();
        Assert.Contains("Rommet finnes ikke.", innhold);
        Assert.Contains(ukjentRomId.ToString(), innhold);
        Assert.Equal(0, romskriver.AntallLagringer);
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

    private static async Task KontrollerRengjøringsstatusAsync(
        HttpResponseMessage svar,
        string forventetStatus,
        bool erKlart)
    {
        using var dokument = JsonDocument.Parse(await svar.Content.ReadAsStringAsync());
        Assert.Equal(forventetStatus, dokument.RootElement.GetProperty("rengjøringsstatus").GetString());
        Assert.Equal(erKlart, dokument.RootElement.GetProperty("erKlartForInnsjekking").GetBoolean());
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

    private sealed class TestRomskriver(bool kanLagre = true, Rom? rom = null, bool lagringskonflikt = false) : IRomskriver
    {
        public Rom? SistLagtTil { get; private set; }

        public int AntallKall { get; private set; }

        public int AntallLagringer { get; private set; }

        public Task<Rom?> HentAsync(Guid romId, CancellationToken avbryt = default) =>
            Task.FromResult(rom?.Id == romId ? rom : null);

        public Task<bool> PrøvLeggTilAsync(Rom rom, CancellationToken avbryt = default)
        {
            AntallKall++;
            SistLagtTil = rom;
            return Task.FromResult(kanLagre);
        }

        public Task LagreEndringerAsync(CancellationToken avbryt = default)
        {
            if (lagringskonflikt)
                throw new RomkonfliktException(new Exception("intern-testdetalj"));
            AntallLagringer++;
            return Task.CompletedTask;
        }
    }
}
