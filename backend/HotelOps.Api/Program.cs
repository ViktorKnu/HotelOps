using System.Text.Json.Serialization;
using HotelOps.Api.Endepunkter;
using HotelOps.Applikasjon.Romadministrasjon;
using HotelOps.Infrastruktur;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.ConfigureHttpJsonOptions(valg =>
    valg.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddProblemDetails(valg =>
{
    valg.CustomizeProblemDetails = kontekst =>
    {
        if (kontekst.ProblemDetails.Status == StatusCodes.Status500InternalServerError)
        {
            kontekst.ProblemDetails.Title = "En uventet feil oppstod.";
            kontekst.ProblemDetails.Detail = "Prøv igjen senere.";
        }
    };
});
builder.Services.AddScoped<Romregistrering>();
builder.Services.AddScoped<Romrengjøring>();
builder.Services.LeggTilInfrastruktur(builder.Configuration);

var app = builder.Build();

app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}
else
{
    app.UseHttpsRedirection();
}

app.MapGet("/api/helse", () => Results.Ok(new { status = "ok" }))
    .WithName("HentHelsestatus");

app.RegistrerRomendepunkter();

app.Run();

public partial class Program
{
}
