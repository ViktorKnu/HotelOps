namespace HotelOps.Domene.Romadministrasjon;

public sealed class Rom
{
    private Rom()
    {
    }

    public Rom(
        string nummer,
        int etasje,
        Beleggsstatus beleggsstatus = Beleggsstatus.Ledig,
        Rengjøringsstatus rengjøringsstatus = Rengjøringsstatus.Ren,
        Driftsstatus driftsstatus = Driftsstatus.Operativ)
    {
        if (string.IsNullOrWhiteSpace(nummer))
        {
            throw new ArgumentException("Romnummer må oppgis.", nameof(nummer));
        }

        Id = Guid.NewGuid();
        Nummer = nummer.Trim();
        Etasje = etasje;
        Beleggsstatus = beleggsstatus;
        Rengjøringsstatus = rengjøringsstatus;
        Driftsstatus = driftsstatus;
    }

    public Guid Id { get; private set; }

    public string Nummer { get; private set; } = null!;

    public int Etasje { get; private set; }

    public Beleggsstatus Beleggsstatus { get; private set; }

    public Rengjøringsstatus Rengjøringsstatus { get; private set; }

    public Driftsstatus Driftsstatus { get; private set; }

    public bool ErKlartForInnsjekking =>
        Beleggsstatus == Beleggsstatus.Ledig &&
        Rengjøringsstatus == Rengjøringsstatus.Ren &&
        Driftsstatus == Driftsstatus.Operativ;

    public void MarkerSomSkitten()
    {
        if (Rengjøringsstatus != Rengjøringsstatus.Ren)
        {
            throw new UgyldigRengjøringsovergangException(
                "Bare et rent rom kan markeres som skittent.");
        }

        Rengjøringsstatus = Rengjøringsstatus.Skitten;
    }

    public void StartRengjøring()
    {
        if (Rengjøringsstatus != Rengjøringsstatus.Skitten)
        {
            throw new UgyldigRengjøringsovergangException(
                "Rengjøring kan bare startes for et skittent rom.");
        }

        Rengjøringsstatus = Rengjøringsstatus.UnderRengjøring;
    }

    public void FullførRengjøring()
    {
        if (Rengjøringsstatus != Rengjøringsstatus.UnderRengjøring)
        {
            throw new UgyldigRengjøringsovergangException(
                "Rengjøring kan bare fullføres når rommet er under rengjøring.");
        }

        Rengjøringsstatus = Rengjøringsstatus.Ren;
    }
}
