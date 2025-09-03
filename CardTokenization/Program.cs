using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddSingleton<IDbService, DbService>();
builder.Services.AddSingleton<IFpeService, FpeNetService>();
builder.Services.AddSingleton<ITokenService, TokenService>();

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


app.MapPost("/tokenize", async ([FromBody] Request req, ITokenService svc) =>
{
    if (req.CardNumbers == null || req.CardNumbers.Count == 0)
        return Results.BadRequest(new { Error = "Card number is required." });

    try
    {
        List<TokenEntity> results = [];
        foreach (var cn in req.CardNumbers)
            results.Add(await svc.TokenizeAsync(cn));

        return Results.Ok(new { Data = results });
    }
    catch (Exception ex)
    {
        return Results.Json(new { Error = ex.Message }, statusCode: 500);
    }
})
 .WithName("tokenize")
 .WithOpenApi();


app.MapPost("/detokenize", async (Request req, ITokenService svc) =>
{
    if (req.Tokens == null || req.Tokens.Count == 0)
        return Results.BadRequest(new { Error = "Token is required." });

    try
    {
        List<CardTokenEntity> results = [];
        foreach (var t in req.Tokens)
        {
            var tokenEntity = await svc.DetokenizeAsync(t);
            if (tokenEntity is not null)
                results.Add(tokenEntity);
        }

        return Results.Ok(new { Data = results });
    }
    catch (Exception ex)
    {
        return Results.Json(new { Error = ex.Message }, statusCode: 500);
    }
})
 .WithName("detokenize")
 .WithOpenApi();

#if !DEBUG

var port = Environment.GetEnvironmentVariable("Port") ?? "8080";
app.Urls.Add($"http://0.0.0.0:{port}");

#endif

app.Run();