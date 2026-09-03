using HotelOps.Domene.Romadministrasjon;

namespace HotelOps.DomeneTester.Romadministrasjon;

public sealed class RomTester
{
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
