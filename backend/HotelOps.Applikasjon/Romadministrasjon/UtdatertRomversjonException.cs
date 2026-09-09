namespace HotelOps.Applikasjon.Romadministrasjon;

public sealed class UtdatertRomversjonException()
    : Exception("Rommet er endret siden du hentet oversikten. Oppdater oversikten og vurder endringen på nytt.");
