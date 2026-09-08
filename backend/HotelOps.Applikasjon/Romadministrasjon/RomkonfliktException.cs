namespace HotelOps.Applikasjon.Romadministrasjon;

public sealed class RomkonfliktException(Exception indreFeil)
    : Exception("Rommet ble endret av en annen bruker. Oppdater oversikten og prøv igjen.", indreFeil);
