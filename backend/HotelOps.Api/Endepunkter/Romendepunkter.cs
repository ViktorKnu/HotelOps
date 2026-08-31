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

        return ruter;
    }
}
