using HotelOps.Domene.Romadministrasjon;

namespace HotelOps.DomeneTester.Romadministrasjon;

public sealed class RomTester
{
    [Fact]
    public void HverGyldigeEndringGirNyVersjonOgsåNårStatusGårTilbakeTilStart()
    {
        var rom = new Rom("101", 1);
        var versjoner = new HashSet<Guid> { rom.Versjon };
        rom.MarkerSomSkitten();
        Assert.True(versjoner.Add(rom.Versjon));
        rom.PlanleggRenhold("Kari", Renholdsprioritet.Haster);
        Assert.True(versjoner.Add(rom.Versjon));
        rom.StartRengjøring();
        Assert.True(versjoner.Add(rom.Versjon));
        rom.FullførRengjøring();
        Assert.True(versjoner.Add(rom.Versjon));
        Assert.DoesNotContain(Guid.Empty, versjoner);
        var siste = rom.Versjon;
        Assert.Throws<UgyldigRengjøringsovergangException>(rom.StartRengjøring);
        Assert.Equal(siste, rom.Versjon);
    }

    [Fact]
    public void RenholdsplanBevaresVedStartOgNullstillesVedFullføring()
    {
        var rom = new Rom("101", 1);
        rom.MarkerSomSkitten();
        rom.PlanleggRenhold("  Kari  ", Renholdsprioritet.Haster);
        rom.StartRengjøring();
        Assert.Equal("Kari", rom.AnsvarligRenholder);
        Assert.Equal(Renholdsprioritet.Haster, rom.Renholdsprioritet);
        rom.FullførRengjøring();
        rom.MarkerSomSkitten();
        Assert.Null(rom.AnsvarligRenholder);
        Assert.Equal(Renholdsprioritet.Normal, rom.Renholdsprioritet);
    }

    [Fact]
    public void RenholdsplanKanEndresOgTildelingFjernesUnderRengjøring()
    {
        var rom = new Rom("101", 1, rengjøringsstatus: Rengjøringsstatus.UnderRengjøring);
        rom.PlanleggRenhold("Kari", Renholdsprioritet.Haster);
        rom.PlanleggRenhold("  ", Renholdsprioritet.Normal);
        Assert.Null(rom.AnsvarligRenholder);
        Assert.Equal(Renholdsprioritet.Normal, rom.Renholdsprioritet);
    }

    [Fact]
    public void UgyldigPlanEndrerIkkeEksisterendeTildeling()
    {
        var rom = new Rom("101", 1, rengjøringsstatus: Rengjøringsstatus.Skitten);
        rom.PlanleggRenhold("Kari", Renholdsprioritet.Haster);
        Assert.Throws<ArgumentException>(() => rom.PlanleggRenhold(new string('a', 101), Renholdsprioritet.Normal));
        Assert.Throws<ArgumentException>(() => rom.PlanleggRenhold("Ola", (Renholdsprioritet)99));
        Assert.Equal("Kari", rom.AnsvarligRenholder);
        Assert.Equal(Renholdsprioritet.Haster, rom.Renholdsprioritet);
        Assert.Throws<UgyldigRengjøringsovergangException>(() =>
            new Rom("102", 1).PlanleggRenhold("Ola", Renholdsprioritet.Normal));
    }

    [Fact]
    public void NyttRomMedStandardstatusErKlartForInnsjekking()
    {
        var rom = new Rom("101", 1);

        Assert.True(rom.ErKlartForInnsjekking);
    }

    [Theory]
    [InlineData(Beleggsstatus.Opptatt, Rengjøringsstatus.Ren, Driftsstatus.Operativ)]
    [InlineData(Beleggsstatus.Ledig, Rengjøringsstatus.Skitten, Driftsstatus.Operativ)]
    [InlineData(Beleggsstatus.Ledig, Rengjøringsstatus.UnderRengjøring, Driftsstatus.Operativ)]
    [InlineData(Beleggsstatus.Ledig, Rengjøringsstatus.Ren, Driftsstatus.UnderVedlikehold)]
    [InlineData(Beleggsstatus.Ledig, Rengjøringsstatus.Ren, Driftsstatus.UteAvDrift)]
    public void RomSomIkkeErLedigRentOgOperativtErIkkeKlartForInnsjekking(
        Beleggsstatus beleggsstatus,
        Rengjøringsstatus rengjøringsstatus,
        Driftsstatus driftsstatus)
    {
        var rom = new Rom("101", 1, beleggsstatus, rengjøringsstatus, driftsstatus);

        Assert.False(rom.ErKlartForInnsjekking);
    }

    [Fact]
    public void RomnummerMåOppgis()
    {
        var feil = Assert.Throws<ArgumentException>(() => new Rom(" ", 1));

        Assert.Equal("nummer", feil.ParamName);
        Assert.StartsWith("Romnummer må oppgis.", feil.Message);
    }

    [Fact]
    public void RomnummerFjernerOverflødigeMellomrom()
    {
        var rom = new Rom(" 101 ", 1);

        Assert.Equal("101", rom.Nummer);
    }

    [Fact]
    public void RengjøringsflytFølgerForventedeStatuser()
    {
        var rom = new Rom("101", 1);

        rom.MarkerSomSkitten();

        Assert.Equal(Rengjøringsstatus.Skitten, rom.Rengjøringsstatus);
        Assert.False(rom.ErKlartForInnsjekking);

        rom.StartRengjøring();

        Assert.Equal(Rengjøringsstatus.UnderRengjøring, rom.Rengjøringsstatus);
        Assert.False(rom.ErKlartForInnsjekking);

        rom.FullførRengjøring();

        Assert.Equal(Rengjøringsstatus.Ren, rom.Rengjøringsstatus);
        Assert.True(rom.ErKlartForInnsjekking);
    }

    [Theory]
    [InlineData(Rengjøringsstatus.Skitten)]
    [InlineData(Rengjøringsstatus.UnderRengjøring)]
    public void BareRentRomKanMarkeresSomSkittent(Rengjøringsstatus opprinneligStatus)
    {
        var rom = new Rom("101", 1, rengjøringsstatus: opprinneligStatus);

        var feil = Assert.Throws<UgyldigRengjøringsovergangException>(rom.MarkerSomSkitten);

        Assert.Equal("Bare et rent rom kan markeres som skittent.", feil.Message);
        Assert.Equal(opprinneligStatus, rom.Rengjøringsstatus);
    }

    [Theory]
    [InlineData(Rengjøringsstatus.Ren)]
    [InlineData(Rengjøringsstatus.UnderRengjøring)]
    public void RengjøringKanBareStartesForSkittentRom(Rengjøringsstatus opprinneligStatus)
    {
        var rom = new Rom("101", 1, rengjøringsstatus: opprinneligStatus);

        var feil = Assert.Throws<UgyldigRengjøringsovergangException>(rom.StartRengjøring);

        Assert.Equal("Rengjøring kan bare startes for et skittent rom.", feil.Message);
        Assert.Equal(opprinneligStatus, rom.Rengjøringsstatus);
    }

    [Theory]
    [InlineData(Rengjøringsstatus.Ren)]
    [InlineData(Rengjøringsstatus.Skitten)]
    public void RengjøringKanBareFullføresNårDenErStartet(Rengjøringsstatus opprinneligStatus)
    {
        var rom = new Rom("101", 1, rengjøringsstatus: opprinneligStatus);

        var feil = Assert.Throws<UgyldigRengjøringsovergangException>(rom.FullførRengjøring);

        Assert.Equal(
            "Rengjøring kan bare fullføres når rommet er under rengjøring.",
            feil.Message);
        Assert.Equal(opprinneligStatus, rom.Rengjøringsstatus);
    }
}
