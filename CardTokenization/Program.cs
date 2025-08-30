using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddSingleton<IFpeService, FpeNetService>();
builder.Services.AddSingleton<ITokenizationService, TokenizationService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.MapGet("/ping", () => Results.Ok(new { ok = true, now = DateTime.UtcNow }))
.WithName("ping")
.WithOpenApi(); ;


app.MapPost("/tokenize", async ([FromBody] TokenRequest req, ITokenizationService svc) =>
{
    if (req?.CardNumber is null) return Results.BadRequest("Card number is required.");
    try
    {
        var result = svc.TokenizeAsync(req.CardNumber);
        return Results.Ok(new TokenizeResponse
        {
            FpeToken = result.FpeToken,
            OpaqueToken = result.OpaqueToken,
            CreatedUtc = result.CreatedUtc
        });
    }
    catch (ArgumentException ex) { return Results.BadRequest(ex.Message); }
    catch (Exception ex) { return Results.Problem(ex.Message, statusCode: 500); }
})
 .WithName("tokenize")
 .WithOpenApi();


app.MapPost("/detokenize", async (TokenRequest req, ITokenizationService svc) =>
{
    if (req?.Token is null) return Results.BadRequest("Card number is required.");
    try
    {
        var pan = svc.DetokenizeAsync(req.Token);
        if (pan is null) return Results.NotFound();
        return Results.Ok(new TokenizeResponse
        {
            Pan = pan.Pan,
            FpeToken = pan.FpeToken,
            OpaqueToken = pan.OpaqueToken,
            CreatedUtc = pan.CreatedUtc
        });
    }
    catch (ArgumentException ex) { return Results.BadRequest(ex.Message); }
    catch (Exception ex) { return Results.Problem(ex.Message, statusCode: 500); }
})
 .WithName("detokenize")
 .WithOpenApi();

#if !DEBUG

var port = Environment.GetEnvironmentVariable("Port") ?? "8080";
app.Urls.Add($"http://0.0.0.0:{port}");

#endif

app.Run();