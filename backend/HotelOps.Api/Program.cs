using HotelOps.Infrastruktur;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.LeggTilInfrastruktur(builder.Configuration);

var app = builder.Build();

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

app.Run();
