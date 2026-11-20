using System.Text;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text.RegularExpressions;

using Google.Apis.Auth;

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

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.PropertyNameCaseInsensitive = true;
});

var app = builder.Build();
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthentication();
app.UseAuthorization();
app.UseWebSockets();

// ── GENERAL ──────────────────────────────────────────────────────────────────

app.MapGet("/", () => "NeonStrike2D Backend API running");

app.MapGet("/me", async (ClaimsPrincipal user, AppDbContext db) =>
{
    var userId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    var username = user.FindFirst(ClaimTypes.Name)?.Value;
    var email = user.FindFirst(ClaimTypes.Email)?.Value;

    // Un token de una cuenta ya borrada no restaura la sesión (issue #280).
    if (!int.TryParse(userId, out var id) || !await db.Users.AnyAsync(u => u.Id == id))
        return Results.Unauthorized();

    return Results.Ok(new { UserId = userId, Username = username, Email = email });
}).RequireAuthorization();

// Borrado de cuenta (issue #280, requisito de Google Play): borra el usuario y todos sus datos
// en una transacción y cierra su WebSocket si está conectado.
app.MapDelete("/me", async (ClaimsPrincipal user, AppDbContext db) =>
{
    var userId = int.Parse(user.FindFirst(ClaimTypes.NameIdentifier)!.Value);
    var username = await db.Users.Where(u => u.Id == userId).Select(u => u.Username).FirstOrDefaultAsync();
    if (username == null) return Results.NotFound();

    await using (var tx = await db.Database.BeginTransactionAsync())
    {
        var sessions = db.GameSessions.Where(s => s.Player1Id == userId || s.Player2Id == userId || s.WinnerId == userId);
        await db.GameResults.Where(r => r.UserId == userId || sessions.Any(s => s.Id == r.GameSessionId)).ExecuteDeleteAsync();
        await sessions.ExecuteDeleteAsync();
        await db.Friendships.Where(f => f.RequesterId == userId || f.AddresseeId == userId).ExecuteDeleteAsync();
        await db.Users.Where(u => u.Id == userId).ExecuteDeleteAsync();
        await tx.CommitAsync();
    }

    await WebSocketHandler.DisconnectUserAsync(username);
    return Results.Ok(new { Success = true });
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

    var username = request.Username.Trim();
    var email = request.Email.Trim();

    // Misma regla que LoginUI.IsValidUsername en el cliente (#208).
    if (!Regex.IsMatch(username, "^[A-Za-z0-9_]{3,16}$"))
        return Results.BadRequest(new RegisterResponse { Success = false, Message = "El nombre de usuario debe tener entre 3 y 16 caracteres: letras, números o _." });

    if (await db.Users.AnyAsync(u => u.Email.ToLower() == email.ToLower()))
        return Results.BadRequest(new RegisterResponse { Success = false, Message = "Ese email ya está registrado." });

    if (await db.Users.AnyAsync(u => u.Username.ToLower() == username.ToLower()))
        return Results.BadRequest(new RegisterResponse { Success = false, Message = "Ese username ya está en uso." });

    var user = new User { Username = username, Email = email };
    var passwordHasher = new PasswordHasher<User>();
    user.PasswordHash = passwordHasher.HashPassword(user, request.Password);

    db.Users.Add(user);
    await db.SaveChangesAsync();

    return Results.Ok(new RegisterResponse { Success = true, Message = "Usuario registrado correctamente." });
});

app.MapPost("/login", async (LoginRequest request, AppDbContext db) =>
{
    if (string.IsNullOrWhiteSpace(request.Email))
        return Results.BadRequest(new LoginResponse { Success = false, Message = "El email o usuario es obligatorio." });

    if (string.IsNullOrWhiteSpace(request.Password))
        return Results.BadRequest(new LoginResponse { Success = false, Message = "La contraseña es obligatoria." });

    var input = request.Email.Trim().ToLower();
    User? user;
    if (input.Contains('@'))
        user = await db.Users.FirstOrDefaultAsync(u => u.Email.ToLower() == input);
    else
        user = await db.Users.FirstOrDefaultAsync(u => u.Username.ToLower() == input);

    if (user == null)
        return Results.BadRequest(new LoginResponse { Success = false, Message = "Credenciales incorrectas." });

    // Las cuentas creadas con Google no tienen contraseña (issue #75).
    if (string.IsNullOrEmpty(user.PasswordHash))
        return Results.BadRequest(new LoginResponse { Success = false, Message = "Esta cuenta usa Google: entra con el botón de Google." });

    var passwordHasher = new PasswordHasher<User>();
    var result = passwordHasher.VerifyHashedPassword(user, user.PasswordHash, request.Password);
    if (result == PasswordVerificationResult.Failed)
        return Results.BadRequest(new LoginResponse { Success = false, Message = "Credenciales incorrectas." });

    return Results.Ok(BuildLoginResponse(user, "Login correcto."));
});

// Inicio de sesión con Google (issue #75). El cliente solo manda el id_token; el email y el
// "sub" salen del token una vez verificado aquí (firma, caducidad, emisor y audiencia).
app.MapPost("/auth/google", async (GoogleLoginRequest request, AppDbContext db) =>
{
    var clientId = builder.Configuration["Google:ClientId"];
    if (string.IsNullOrWhiteSpace(clientId))
        return Results.Json(new LoginResponse { Success = false, Message = "El inicio de sesión con Google no está disponible." }, statusCode: StatusCodes.Status503ServiceUnavailable);

    if (string.IsNullOrWhiteSpace(request.IdToken))
        return Results.BadRequest(new LoginResponse { Success = false, Message = "Falta el token de Google." });

    GoogleJsonWebSignature.Payload payload;
    try
    {
        payload = await GoogleJsonWebSignature.ValidateAsync(request.IdToken, new GoogleJsonWebSignature.ValidationSettings
        {
            Audience = new[] { clientId }
        });
    }
    catch (InvalidJwtException)
    {
        return Results.BadRequest(new LoginResponse { Success = false, Message = "El token de Google no es válido." });
    }

    if (string.IsNullOrEmpty(payload.Email) || !payload.EmailVerified)
        return Results.BadRequest(new LoginResponse { Success = false, Message = "La cuenta de Google no tiene un email verificado." });

    var isNewAccount = false;
    var user = await db.Users.FirstOrDefaultAsync(u => u.GoogleSubject == payload.Subject);
    if (user == null)
    {
        var email = payload.Email.Trim();
        user = await db.Users.FirstOrDefaultAsync(u => u.Email.ToLower() == email.ToLower());

        if (user != null)
        {
            // Ya había una cuenta con ese email: se vincula, salvo que ya esté unida a otra cuenta de Google.
            if (user.GoogleSubject != null)
                return Results.BadRequest(new LoginResponse { Success = false, Message = "Ese email ya está vinculado a otra cuenta de Google." });

            user.GoogleSubject = payload.Subject;
        }
        else
        {
            user = new User
            {
                Username = await ProposeUsernameAsync(db, payload.Name ?? payload.GivenName ?? email.Split('@')[0]),
                Email = email,
                PasswordHash = null,
                GoogleSubject = payload.Subject
            };
            db.Users.Add(user);
            isNewAccount = true;
        }

        await db.SaveChangesAsync();
    }

    var response = BuildLoginResponse(user, "Login correcto.");
    response.IsNewAccount = isNewAccount;
    return Results.Ok(response);
});

// Cambio de nombre de usuario, sobre todo para las cuentas de Google, cuyo nombre se propone
// automáticamente (issue #75). Devuelve un JWT nuevo porque el nombre va dentro del token.
app.MapPut("/me/username", async (ChangeUsernameRequest request, ClaimsPrincipal principal, AppDbContext db) =>
{
    var userId = int.Parse(principal.FindFirst(ClaimTypes.NameIdentifier)!.Value);
    var user = await db.Users.FirstOrDefaultAsync(u => u.Id == userId);
    if (user == null) return Results.Unauthorized();

    var username = (request.Username ?? string.Empty).Trim();
    if (!IsValidUsername(username))
        return Results.BadRequest(new LoginResponse { Success = false, Message = "El nombre de usuario debe tener entre 3 y 16 caracteres: letras, números o _." });

    if (await db.Users.AnyAsync(u => u.Id != userId && u.Username.ToLower() == username.ToLower()))
        return Results.BadRequest(new LoginResponse { Success = false, Message = "Ese username ya está en uso." });

    var oldUsername = user.Username;
    user.Username = username;
    await db.SaveChangesAsync();

    // El WebSocket está registrado con el nombre viejo: se cierra y el cliente vuelve a conectar con el nuevo.
    if (oldUsername != username)
        await WebSocketHandler.DisconnectUserAsync(oldUsername);

    return Results.Ok(BuildLoginResponse(user, "Nombre de usuario cambiado."));
}).RequireAuthorization();

// ── RANKING ──────────────────────────────────────────────────────────────────

app.MapPost("/match/result", async (MatchResultRequest request, ClaimsPrincipal user, AppDbContext db) =>
{
    // El resultado siempre es del usuario del token, nunca del id que llegue en el cuerpo (issue #288).
    var userId = int.Parse(user.FindFirst(ClaimTypes.NameIdentifier)!.Value);

    var session = new GameSession
    {
        RoomId = $"solo_{userId}_{DateTime.UtcNow.Ticks}",
        Player1Id = userId,
        Player2Id = userId,
        WinnerId = null,
        StartedAt = DateTime.UtcNow,
        EndedAt = DateTime.UtcNow,
        Status = "finished"
    };

    db.GameSessions.Add(session);
    await db.SaveChangesAsync();

    db.GameResults.Add(new GameResult
    {
        UserId = userId,
        GameSessionId = session.Id,
        Won = false,
        RoundsWon = request.BestWave,
        RoundsLost = 0,
        Score = request.Score,
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
        BestWave = g.Max(r => r.RoundsWon),
        Score = g.Max(r => r.Score)
    })
    .OrderByDescending(r => r.Score)
    .ThenByDescending(r => r.BestWave)
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

app.MapGet("/friends/online", async (ClaimsPrincipal user, AppDbContext db) =>
{
    var userId = int.Parse(user.FindFirst(ClaimTypes.NameIdentifier)!.Value);

    var friends = await db.Friendships
        .Where(f => (f.RequesterId == userId || f.AddresseeId == userId) && f.Status == FriendshipStatus.Accepted)
        .Select(f => f.RequesterId == userId ? f.Addressee.Username : f.Requester.Username)
        .ToListAsync();

    var onlineFriends = friends.Where(f => WebSocketHandler.IsUserOnline(f)).ToList();

    return Results.Ok(onlineFriends);
}).RequireAuthorization();

// ── USUARIOS ─────────────────────────────────────────────────────────────────

app.MapGet("/users/{username}", async (string username, AppDbContext db) =>
{
    var user = await db.Users.FirstOrDefaultAsync(u => u.Username == username);
    if (user == null) return Results.NotFound();
    return Results.Ok(new { userId = user.Id, username = user.Username });
}).RequireAuthorization();

// ── WEBSOCKET ────────────────────────────────────────────────────────────────

app.Map("/ws", WebSocketHandler.Handle);

// Solo en local: en produccion no se expone quien esta conectado (issue #281).
if (app.Environment.IsDevelopment())
    app.MapGet("/debug/online", () => Results.Ok(WebSocketHandler.GetOnlineUsers()));

app.Run();

// ── AYUDAS ───────────────────────────────────────────────────────────────────

// Misma regla que LoginUI.IsValidUsername en el cliente (#208).
static bool IsValidUsername(string username) => Regex.IsMatch(username, "^[A-Za-z0-9_]{3,16}$");

// Mismo JWT para el login con contraseña y el de Google.
LoginResponse BuildLoginResponse(User user, string message)
{
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
        expires: DateTime.UtcNow.AddDays(30),
        signingCredentials: creds
    );

    return new LoginResponse
    {
        Success = true,
        Message = message,
        UserId = user.Id,
        Username = user.Username,
        Email = user.Email,
        Token = new JwtSecurityTokenHandler().WriteToken(token)
    };
}

// Nombre propuesto para una cuenta nueva de Google: el nombre de Google limpiado a la regla de #208,
// con un sufijo numérico si ya existe.
static async Task<string> ProposeUsernameAsync(AppDbContext db, string source)
{
    var baseName = Regex.Replace(source.Normalize(System.Text.NormalizationForm.FormD), "[^A-Za-z0-9_]", "");
    if (baseName.Length < 3) baseName = "Jugador";
    if (baseName.Length > 12) baseName = baseName[..12];

    var candidate = baseName;
    for (var i = 1; await db.Users.AnyAsync(u => u.Username.ToLower() == candidate.ToLower()); i++)
        candidate = baseName + i;

    return candidate;
}