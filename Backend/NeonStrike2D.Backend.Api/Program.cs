using System.Text;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

using NeonStrike2D.Backend.Api.Data;
using NeonStrike2D.Backend.Api.Dtos;
using NeonStrike2D.Backend.Api.Hubs;
using NeonStrike2D.Backend.Api.Models;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var jwtKey = builder.Configuration["Jwt:Key"];
var jwtIssuer = builder.Configuration["Jwt:Issuer"];
var jwtAudience = builder.Configuration["Jwt:Audience"];

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtIssuer,
            ValidAudience = jwtAudience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey!))
        };
    });

builder.Services.AddAuthorization();
builder.Services.AddSignalR();

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthentication();
app.UseAuthorization();

// ── GENERAL ──────────────────────────────────────────────────────────────────

app.MapGet("/", () => "NeonStrike2D Backend API running");

app.MapGet("/me", (ClaimsPrincipal user) =>
{
    var userId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    var username = user.FindFirst(ClaimTypes.Name)?.Value;
    var email = user.FindFirst(ClaimTypes.Email)?.Value;

    return Results.Ok(new { UserId = userId, Username = username, Email = email });
}).RequireAuthorization();

// ── AUTH ─────────────────────────────────────────────────────────────────────

app.MapPost("/register", async (RegisterRequest request, AppDbContext db) =>
{
    if (string.IsNullOrWhiteSpace(request.Username))
        return Results.BadRequest(new RegisterResponse { Success = false, Message = "El username es obligatorio." });

    if (string.IsNullOrWhiteSpace(request.Email))
        return Results.BadRequest(new RegisterResponse { Success = false, Message = "El email es obligatorio." });

    if (string.IsNullOrWhiteSpace(request.Password))
        return Results.BadRequest(new RegisterResponse { Success = false, Message = "La contraseña es obligatoria." });

    if (await db.Users.AnyAsync(u => u.Email == request.Email))
        return Results.BadRequest(new RegisterResponse { Success = false, Message = "Ese email ya está registrado." });

    if (await db.Users.AnyAsync(u => u.Username == request.Username))
        return Results.BadRequest(new RegisterResponse { Success = false, Message = "Ese username ya está en uso." });

    var user = new User { Username = request.Username, Email = request.Email };
    var passwordHasher = new PasswordHasher<User>();
    user.PasswordHash = passwordHasher.HashPassword(user, request.Password);

    db.Users.Add(user);
    await db.SaveChangesAsync();

    return Results.Ok(new RegisterResponse { Success = true, Message = "Usuario registrado correctamente." });
});

app.MapPost("/login", async (LoginRequest request, AppDbContext db) =>
{
    if (string.IsNullOrWhiteSpace(request.Email))
        return Results.BadRequest(new LoginResponse { Success = false, Message = "El email es obligatorio." });

    if (string.IsNullOrWhiteSpace(request.Password))
        return Results.BadRequest(new LoginResponse { Success = false, Message = "La contraseña es obligatoria." });

    var user = await db.Users.FirstOrDefaultAsync(u => u.Email == request.Email);
    if (user == null)
        return Results.BadRequest(new LoginResponse { Success = false, Message = "Credenciales incorrectas." });

    var passwordHasher = new PasswordHasher<User>();
    var result = passwordHasher.VerifyHashedPassword(user, user.PasswordHash, request.Password);
    if (result == PasswordVerificationResult.Failed)
        return Results.BadRequest(new LoginResponse { Success = false, Message = "Credenciales incorrectas." });

    var claims = new[]
    {
        new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
        new Claim(ClaimTypes.Name, user.Username),
        new Claim(ClaimTypes.Email, user.Email)
    };

    var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!));
    var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
    var token = new JwtSecurityToken(
        issuer: builder.Configuration["Jwt:Issuer"],
        audience: builder.Configuration["Jwt:Audience"],
        claims: claims,
        expires: DateTime.UtcNow.AddHours(2),
        signingCredentials: creds
    );

    return Results.Ok(new LoginResponse
    {
        Success = true,
        Message = "Login correcto.",
        UserId = user.Id,
        Username = user.Username,
        Email = user.Email,
        Token = new JwtSecurityTokenHandler().WriteToken(token)
    });
});

// ── RANKING ──────────────────────────────────────────────────────────────────

// Guarda el resultado de una partida
app.MapPost("/match/result", async (MatchResultRequest request, AppDbContext db) =>
{
    var session = new GameSession
    {
        RoomId = request.RoomId,
        Player1Id = request.Player1Id,
        Player2Id = request.Player2Id,
        WinnerId = request.WinnerId,
        StartedAt = request.StartedAt,
        EndedAt = DateTime.UtcNow,
        Status = "finished"
    };

    db.GameSessions.Add(session);
    await db.SaveChangesAsync();

    // Resultado jugador 1
    db.GameResults.Add(new GameResult
    {
        UserId = request.Player1Id,
        GameSessionId = session.Id,
        Won = request.WinnerId == request.Player1Id,
        RoundsWon = request.Player1RoundsWon,
        RoundsLost = request.Player2RoundsWon,
        PlayedAt = DateTime.UtcNow
    });

    // Resultado jugador 2
    db.GameResults.Add(new GameResult
    {
        UserId = request.Player2Id,
        GameSessionId = session.Id,
        Won = request.WinnerId == request.Player2Id,
        RoundsWon = request.Player2RoundsWon,
        RoundsLost = request.Player1RoundsWon,
        PlayedAt = DateTime.UtcNow
    });

    await db.SaveChangesAsync();

    return Results.Ok(new { Success = true, Message = "Resultado guardado correctamente." });
}).RequireAuthorization();

// Devuelve el top 10 de jugadores
app.MapGet("/ranking", async (AppDbContext db) =>
{
    var ranking = await db.GameResults
        .GroupBy(r => r.UserId)
        .Select(g => new
        {
            UserId = g.Key,
            Username = g.First().User.Username,
            TotalGames = g.Count(),
            Wins = g.Count(r => r.Won),
            Losses = g.Count(r => !r.Won)
        })
        .OrderByDescending(r => r.Wins)
        .Take(10)
        .ToListAsync();

    return Results.Ok(ranking);
});

// ── SIGNALR ──────────────────────────────────────────────────────────────────

app.MapHub<GameHub>("/gamehub");

app.Run();