using HotelOps.Applikasjon.Romadministrasjon;
using HotelOps.Domene.Romadministrasjon;
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

        ruter.MapPatch(
            "/api/rom/{romId:guid}/rengjoring/marker-skitten",
            async (Guid romId, Romrengjøring romrengjøring, CancellationToken avbryt) =>
                await UtførRengjøringsendringAsync(
                    romId,
                    () => romrengjøring.MarkerSomSkittenAsync(romId, avbryt)))
            .WithName("MarkerRomSomSkittent")
            .WithTags("Rom")
            .Produces<RomDto>()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status500InternalServerError);

        ruter.MapPatch(
            "/api/rom/{romId:guid}/rengjoring/start",
            async (Guid romId, Romrengjøring romrengjøring, CancellationToken avbryt) =>
                await UtførRengjøringsendringAsync(
                    romId,
                    () => romrengjøring.StartAsync(romId, avbryt)))
            .WithName("StartRengjøringAvRom")
            .WithTags("Rom")
            .Produces<RomDto>()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status500InternalServerError);

        ruter.MapPatch(
            "/api/rom/{romId:guid}/rengjoring/fullfor",
            async (Guid romId, Romrengjøring romrengjøring, CancellationToken avbryt) =>
                await UtførRengjøringsendringAsync(
                    romId,
                    () => romrengjøring.FullførAsync(romId, avbryt)))
            .WithName("FullførRengjøringAvRom")
            .WithTags("Rom")
            .Produces<RomDto>()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status500InternalServerError);

        ruter.MapPatch("/api/rom/{romId:guid}/rengjoring/planlegg", async (
            Guid romId, PlanleggRenholdForespørsel forespørsel,
            Romrengjøring romrengjøring, CancellationToken avbryt) =>
        {
            var feil = new Dictionary<string, string[]>();
            if (forespørsel.Prioritet is not ("Normal" or "Haster"))
                feil["prioritet"] = ["Velg Normal eller Haster."];
            if (forespørsel.AnsvarligRenholder?.Trim().Length > 100)
                feil["ansvarligRenholder"] = ["Navnet kan ha maksimalt 100 tegn."];
            if (feil.Count > 0)
                return Results.ValidationProblem(feil, title: "Renholdet kunne ikke planlegges.");

            var prioritet = Enum.Parse<Renholdsprioritet>(forespørsel.Prioritet!);
            return await UtførRengjøringsendringAsync(romId,
                () => romrengjøring.PlanleggAsync(romId, forespørsel.AnsvarligRenholder, prioritet, avbryt));
        })
        .WithName("PlanleggRenhold")
        .WithTags("Rom")
        .Produces<RomDto>()
        .ProducesValidationProblem()
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status409Conflict)
        .ProducesProblem(StatusCodes.Status500InternalServerError);

        return ruter;
    }

    private static async Task<IResult> UtførRengjøringsendringAsync(
        Guid romId,
        Func<Task<RomDto?>> handling)
    {
        try
        {
            var rom = await handling();

            return rom is null
                ? Results.Problem(
                    statusCode: StatusCodes.Status404NotFound,
                    title: "Rommet finnes ikke.",
                    detail: $"Fant ikke rom med ID {romId}.")
                : Results.Ok(rom);
        }
        catch (RomkonfliktException feil)
        {
            return Results.Problem(
                statusCode: StatusCodes.Status409Conflict,
                title: "Rommet er endret av en annen bruker.",
                detail: feil.Message);
        }
        catch (UgyldigRengjøringsovergangException feil)
        {
            return Results.Problem(
                statusCode: StatusCodes.Status409Conflict,
                title: "Rengjøringsstatusen kan ikke endres.",
                detail: feil.Message);
        }
    }
}
