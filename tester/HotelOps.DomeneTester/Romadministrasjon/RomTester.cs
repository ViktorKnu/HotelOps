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
}
