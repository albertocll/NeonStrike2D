namespace NeonStrike2D.Backend.Api.Security;

// Códigos de error que acompañan al mensaje en las respuestas: el cliente 1.0.1 los traduce
// y la 1.0.0 sigue enseñando el mensaje.
public static class ErrorCodes
{
    public const string UsernameRequired = "USERNAME_REQUIRED";
    public const string EmailRequired = "EMAIL_REQUIRED";
    public const string PasswordRequired = "PASSWORD_REQUIRED";
    public const string LoginRequired = "LOGIN_REQUIRED";
    public const string InvalidUsername = "INVALID_USERNAME";
    public const string InvalidEmail = "INVALID_EMAIL";
    public const string InvalidPassword = "INVALID_PASSWORD";
    public const string EmailTaken = "EMAIL_TAKEN";
    public const string UsernameTaken = "USERNAME_TAKEN";
    public const string BadCredentials = "BAD_CREDENTIALS";
    public const string RateLimited = "RATE_LIMITED";
    public const string ScoreRejected = "SCORE_REJECTED";
    public const string UserNotFound = "USER_NOT_FOUND";
    public const string CannotAddSelf = "CANNOT_ADD_SELF";
    public const string RequestExists = "REQUEST_EXISTS";
    public const string RequestNotFound = "REQUEST_NOT_FOUND";
    public const string PlayerOffline = "PLAYER_OFFLINE";
}
