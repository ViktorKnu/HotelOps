namespace HotelOps.Domene.Romadministrasjon;

public sealed class UgyldigRengjøringsovergangException(string melding)
    : InvalidOperationException(melding);
