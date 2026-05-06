using System.Text;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.AspNetCore.SignalR;

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

app.MapPost("/match/result", async (MatchResultRequest request, AppDbContext db) =>
{
    var session = new GameSession
    {
        RoomId = $"solo_{request.UserId}_{DateTime.UtcNow.Ticks}",
        Player1Id = request.UserId,
        Player2Id = request.UserId,
        WinnerId = null,
        StartedAt = DateTime.UtcNow,
        EndedAt = DateTime.UtcNow,
        Status = "finished"
    };

    db.GameSessions.Add(session);
    await db.SaveChangesAsync();

    db.GameResults.Add(new GameResult
    {
        UserId = request.UserId,
        GameSessionId = session.Id,
        Won = false,
        RoundsWon = request.BestWave,
        RoundsLost = 0,
        PlayedAt = DateTime.UtcNow
    });

    await db.SaveChangesAsync();

    return Results.Ok(new { Success = true });
}).RequireAuthorization();

app.MapGet("/ranking", async (AppDbContext db) =>
{
    var ranking = await db.GameResults
        .GroupBy(r => r.UserId)
        .Select(g => new
        {
            UserId = g.Key,
            Username = g.First().User.Username,
            BestWave = g.Max(r => r.RoundsWon)
        })
        .OrderByDescending(r => r.BestWave)
        .Take(10)
        .ToListAsync();

    return Results.Ok(ranking);
});

// ── AMIGOS ───────────────────────────────────────────────────────────────────

app.MapPost("/friends/request/{username}", async (string username, ClaimsPrincipal user, AppDbContext db) =>
{
    var requesterId = int.Parse(user.FindFirst(ClaimTypes.NameIdentifier)!.Value);

    var addressee = await db.Users.FirstOrDefaultAsync(u => u.Username == username);
    if (addressee == null)
        return Results.BadRequest(new { Success = false, Message = "Usuario no encontrado." });

    if (addressee.Id == requesterId)
        return Results.BadRequest(new { Success = false, Message = "No puedes añadirte a ti mismo." });

    var exists = await db.Friendships.AnyAsync(f =>
        (f.RequesterId == requesterId && f.AddresseeId == addressee.Id) ||
        (f.RequesterId == addressee.Id && f.AddresseeId == requesterId));

    if (exists)
        return Results.BadRequest(new { Success = false, Message = "Ya existe una solicitud entre estos usuarios." });

    db.Friendships.Add(new Friendship
    {
        RequesterId = requesterId,
        AddresseeId = addressee.Id,
        Status = FriendshipStatus.Pending
    });

    await db.SaveChangesAsync();

    return Results.Ok(new { Success = true, Message = "Solicitud enviada." });
}).RequireAuthorization();

app.MapPost("/friends/accept/{requesterId}", async (int requesterId, ClaimsPrincipal user, AppDbContext db) =>
{
    var addresseeId = int.Parse(user.FindFirst(ClaimTypes.NameIdentifier)!.Value);

    var friendship = await db.Friendships.FirstOrDefaultAsync(f =>
        f.RequesterId == requesterId && f.AddresseeId == addresseeId && f.Status == FriendshipStatus.Pending);

    if (friendship == null)
        return Results.NotFound(new { Success = false, Message = "Solicitud no encontrada." });

    friendship.Status = FriendshipStatus.Accepted;
    await db.SaveChangesAsync();

    return Results.Ok(new { Success = true, Message = "Solicitud aceptada." });
}).RequireAuthorization();

app.MapGet("/friends/online", async (ClaimsPrincipal user, AppDbContext db, IHubContext<GameHub> hubContext) =>
{
    var userId = int.Parse(user.FindFirst(ClaimTypes.NameIdentifier)!.Value);

    var friends = await db.Friendships
        .Where(f => (f.RequesterId == userId || f.AddresseeId == userId) && f.Status == FriendshipStatus.Accepted)
        .Select(f => f.RequesterId == userId ? f.Addressee.Username : f.Requester.Username)
        .ToListAsync();

    var onlineFriends = friends.Where(f => GameHub.ConnectedUsers.ContainsKey(f)).ToList();

    return Results.Ok(onlineFriends);
}).RequireAuthorization();

// ── USUARIOS ─────────────────────────────────────────────────────────────────

app.MapGet("/users/{username}", async (string username, AppDbContext db) =>
{
    var user = await db.Users.FirstOrDefaultAsync(u => u.Username == username);
    if (user == null) return Results.NotFound();
    return Results.Ok(new { userId = user.Id, username = user.Username });
}).RequireAuthorization();

// ── SIGNALR ──────────────────────────────────────────────────────────────────

app.MapHub<GameHub>("/gamehub");

app.Run();
