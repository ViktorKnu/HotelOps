using HotelOps.Applikasjon.Romadministrasjon;
using Microsoft.AspNetCore.Http.HttpResults;

namespace HotelOps.Api.Endepunkter;

public static class Romendepunkter
{
    public static IEndpointRouteBuilder RegistrerRomendepunkter(this IEndpointRouteBuilder ruter)
    {
        ruter.MapGet("/api/rom", async Task<Ok<IReadOnlyList<RomDto>>> (
            IRomleser romleser,
            CancellationToken avbryt) =>
        {
            var rom = await romleser.HentAlleAsync(avbryt);
            IReadOnlyList<RomDto> oversikt = rom.Select(RomDto.FraRom).ToArray();

            return TypedResults.Ok(oversikt);
        })
        .WithName("HentRomoversikt")
        .WithTags("Rom")
        .ProducesProblem(StatusCodes.Status500InternalServerError);

        ruter.MapPost("/api/rom", async (
            OpprettRomForespørsel forespørsel,
            Romregistrering romregistrering,
            CancellationToken avbryt) =>
        {
            if (string.IsNullOrWhiteSpace(forespørsel.Nummer))
            {
                return Results.ValidationProblem(
                    new Dictionary<string, string[]>
                    {
                        ["nummer"] = ["Romnummer må oppgis."]
                    },
                    title: "Rommet kunne ikke registreres.");
            }

            var rom = await romregistrering.PrøvOpprettAsync(
                forespørsel.Nummer,
                forespørsel.Etasje,
                avbryt);

            if (rom is null)
            {
                return Results.Problem(
                    statusCode: StatusCodes.Status409Conflict,
                    title: "Romnummeret er allerede i bruk.",
                    detail: $"Rom {forespørsel.Nummer.Trim()} er allerede registrert.");
            }

            return Results.Created("/api/rom", rom);
        })
        .WithName("OpprettRom")
        .WithTags("Rom")
        .Produces<RomDto>(StatusCodes.Status201Created)
        .ProducesValidationProblem()
        .ProducesProblem(StatusCodes.Status409Conflict)
        .ProducesProblem(StatusCodes.Status500InternalServerError);

        return ruter;
    }
}
