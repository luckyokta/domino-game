using DominoGame.Api.Hubs;
using DominoGame.Api.Services;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<GameSessionStore>();
builder.Services.AddControllers();
builder.Services.AddSignalR();

// Only needed when the React app is served from a different origin than the API
// (e.g. you deploy them separately). In local dev the Vite proxy makes this unnecessary.
string[] allowedOrigins =
    builder.Configuration.GetSection("Cors:Origins").Get<string[]>() ?? new[] { "http://localhost:5173" };

builder.Services.AddCors(options =>
{
    options.AddPolicy("client", policy => policy
        .WithOrigins(allowedOrigins)
        .AllowAnyHeader()
        .AllowAnyMethod()
        .AllowCredentials());
});

WebApplication app = builder.Build();

app.UseCors("client");

// Serves the built React app from wwwroot (after `npm run build` in /client).
app.UseDefaultFiles();
app.UseStaticFiles();

app.MapControllers();
app.MapHub<GameHub>("/hubs/game");
app.MapGet("/health", () => Results.Ok(new { status = "ok" }));
app.MapFallbackToFile("index.html");

app.Run();
