using System.Collections.Generic;

// Tabla única de textos del juego: clave -> (español, inglés).
// Textos en Orbitronio: en minúscula, sin signos de puntuación y sin ñ ni tildes.
// Textos en Roboto o LiberationSans: escritura normal, con mayúsculas, signos y ñ.
// Los nombres de niveles, personajes y jefes no se traducen y no están aquí.
public static class LocalizedTexts
{
    private static readonly Dictionary<string, (string es, string en)> Texts = new()
    {
        // Menú principal (Orbitronio)
        { "menu.play", ("jugar", "play") },
        { "menu.login", ("iniciar sesion", "login") },
        { "menu.signup", ("registrarse", "sign up") },
        { "menu.options", ("opciones", "options") },
        { "menu.about", ("acerca de", "about") },
        { "menu.exit", ("salir", "exit") },
        { "common.close", ("cerrar", "close") },
        { "common.back", ("volver", "back") },
        { "register.title", ("registro", "sign up") },

        // Menú principal (Roboto)
        { "menu.guest", ("JUGAR COMO INVITADO", "PLAY AS GUEST") },

        // Opciones (Orbitronio)
        { "options.music", ("musica", "music") },
        { "options.sfx", ("efectos", "effects") },
        { "options.leftHanded", ("modo zurdo", "left handed") },
        { "options.logout", ("cerrar sesion", "log out") },
        { "options.deleteAccount", ("borrar cuenta", "delete account") },
        { "options.privacy", ("politica de privacidad", "privacy policy") },
        { "options.terms", ("terminos de uso", "terms of use") },

        // Diálogo de borrar cuenta (Orbitronio)
        { "delete.title", ("borrar cuenta", "delete account") },
        { "delete.body", ("tu cuenta y tus datos se borraran para siempre", "your account and data will be deleted forever") },
        { "delete.cancel", ("cancelar", "cancel") },
        { "delete.confirm", ("borrar", "delete") },
        { "delete.working", ("borrando", "deleting") },
        { "delete.error", ("no se pudo borrar la cuenta revisa tu conexion", "could not delete the account check your connection") },

        // Salir del juego (Orbitronio)
        { "exit.title", ("salir del juego", "exit game") },
        { "exit.yes", ("si", "yes") },
        { "exit.no", ("no", "no") },

        // Login y registro (Roboto y LiberationSans)
        { "login.userLabel", ("Correo o usuario", "Email or username") },
        { "login.passwordLabel", ("Contraseña", "Password") },
        { "login.noAccount", ("¿No tienes cuenta?", "No account?") },
        { "login.remember", ("Recordar cuenta", "Remember me") },
        { "register.usernameLabel", ("Usuario", "Username") },
        { "register.emailLabel", ("Correo", "Email") },
        { "register.haveAccount", ("¿Ya tienes cuenta?", "Have an account?") },
        { "placeholder.userOrEmail", ("correo o usuario", "email or username") },
        { "placeholder.password", ("contraseña", "password") },
        { "placeholder.username", ("usuario", "username") },
        { "placeholder.email", ("correo", "email") },
        { "placeholder.friendName", ("nombre de usuario", "username") },

        // Ventana de escritura (botones en Orbitronio, nombre del campo en la fuente del campo)
        { "entry.accept", ("aceptar", "accept") },
        { "entry.cancel", ("cancelar", "cancel") },
        { "entry.friendName", ("Nombre de usuario", "Username") },

        // Mensajes de login y registro (Roboto)
        { "msg.fillFields", ("Rellena todos los campos.", "Fill in all fields.") },
        { "msg.connecting", ("Conectando...", "Connecting...") },
        { "msg.welcome", ("¡Bienvenido, {0}!", "Welcome, {0}!") },
        { "msg.connectError", ("No se pudo conectar con el servidor.", "Could not connect to the server.") },
        { "msg.usernameRule", ("El nombre de usuario debe tener entre 3 y 16 caracteres: letras, números o _.", "Username must be 3 to 16 characters: letters, numbers or _.") },
        { "msg.registering", ("Registrando...", "Signing up...") },
        { "msg.registered", ("Registro correcto. Ahora inicia sesión.", "Account created. Now log in.") },
        { "msg.registerError", ("No se pudo completar el registro.", "Sign up failed.") },

        // Mensajes del servidor, traducidos en el cliente (Roboto)
        { "server.usernameRequired", ("El nombre de usuario es obligatorio.", "Username is required.") },
        { "server.emailRequired", ("El correo es obligatorio.", "Email is required.") },
        { "server.passwordRequired", ("La contraseña es obligatoria.", "Password is required.") },
        { "server.emailTaken", ("Ese correo ya está registrado.", "That email is already registered.") },
        { "server.usernameTaken", ("Ese nombre de usuario ya está en uso.", "That username is already taken.") },
        { "server.registered", ("Usuario registrado correctamente.", "Account created.") },
        { "server.loginRequired", ("El correo o usuario es obligatorio.", "Email or username is required.") },
        { "server.badCredentials", ("Credenciales incorrectas.", "Wrong email, username or password.") },
        { "server.loginOk", ("Sesión iniciada.", "Logged in.") },
        { "server.playerOffline", ("El jugador no está conectado.", "That player is not online.") },

        // Selección de personaje (Orbitronio)
        { "select.title", ("elige personaje", "choose character") },
        { "select.confirm", ("elegir", "select") },
        { "role.balanced", ("dron ia equilibrado", "balanced ai drone") },
        { "role.stealth", ("dron ia sigiloso", "stealth ai drone") },
        { "role.assault", ("dron ia de asalto", "assault ai drone") },
        { "stat.attack", ("ataque", "attack") },
        { "stat.defense", ("defensa", "defense") },
        { "stat.fireRate", ("cadencia", "fire rate") },
        { "stat.speed", ("velocidad", "speed") },

        // Amigos e invitaciones (Orbitronio)
        { "friends.title", ("amigos", "friends") },
        { "friends.invite", ("invitar", "invite") },
        { "friends.add", ("agregar", "add") },
        { "friends.decline", ("rechazar", "decline") },
        { "friends.accept", ("aceptar", "accept") },
        { "friends.invites", ("invitaciones", "invites") },
        { "friends.request", ("solicitud de amistad", "friend request") },
        { "friends.challenge", ("desafiar", "challenge") },

        // Amigos e invitaciones (Roboto)
        { "friends.none", ("Tus amigos están en el nexo... o quizás no existen.", "Your friends are in the nexus... or maybe they don't exist.") },
        { "mp.sendingInvite", ("Enviando invitación...", "Sending invite...") },
        { "mp.sendingRequest", ("Enviando solicitud...", "Sending request...") },
        { "mp.requestError", ("No se pudo enviar la solicitud.", "Could not send the request.") },
        { "mp.requestSent", ("¡Solicitud enviada!", "Request sent!") },
        { "mp.friendRequest", ("{0} quiere ser tu amigo", "{0} wants to be your friend") },
        { "mp.waiting", ("Esperando respuesta...", "Waiting for a reply...") },
        { "mp.declined", ("Invitación rechazada.", "Invite declined.") },
        { "mp.invited", ("{0} te ha invitado a jugar", "{0} invited you to play") },

        // Pantalla de carga (Orbitronio)
        { "loading.title", ("cargando", "loading") },
        { "loading.heart", ("corazon cura 20 de vida", "heart heals 20 hp") },
        { "loading.chip", ("chip suma 100 puntos", "chip gives 100 points") },
        { "loading.crystal", ("cristal suma 150 puntos", "crystal gives 150 points") },
        { "loading.shield", ("escudo da invulnerabilidad 5 seg", "shield makes you invincible 5 sec") },
        { "loading.heavy", ("arma pesada mayor cadencia 8 seg", "heavy weapon faster fire 8 sec") },
        { "loading.triple", ("disparo triple lanza 3 balas 8 seg", "triple shot fires 3 bullets 8 sec") },
        { "loading.speed", ("velocidad corres mas rapido 8 seg", "speed boost move faster 8 sec") },
        { "loading.dash", ("dash con espacio en pc o click en pantalla", "dash with space on pc or tap the screen") },

        // HUD (Roboto)
        { "hud.hp", ("Vida {0}", "HP {0}") },
        { "hud.score", ("Puntos {0}", "Score {0}") },
        { "hud.wave", ("Oleada {0}", "Wave {0}") },

        // Pausa (Orbitronio)
        { "pause.title", ("pausa", "pause") },
        { "pause.resume", ("reanudar", "resume") },
        { "pause.restart", ("reiniciar", "restart") },
        { "pause.home", ("inicio", "menu") },
        { "pause.mute", ("silenciar", "mute") },

        // Fin de la partida (Roboto)
        { "gameOver.title", ("Fin de la partida", "Game over") },
        { "gameOver.restart", ("Reiniciar", "Restart") },
        { "gameOver.home", ("Inicio", "Menu") },
        { "gameOver.scores", ("Puntuaciones", "Scores") },
        { "gameOver.wave", ("Oleada alcanzada {0}", "Wave reached {0}") },
        { "gameOver.score", ("Puntos {0}", "Score {0}") },

        // Ranking
        { "ranking.title", ("ranking", "ranking") },
        { "ranking.loading", ("Cargando...", "Loading...") },
        { "ranking.noSession", ("Ranking no disponible sin sesión iniciada.", "Log in to see the ranking.") },
        { "ranking.noResponse", ("No se pudo conectar con el servidor.", "Could not connect to the server.") },
        { "ranking.error", ("No se pudo cargar el ranking.", "Could not load the ranking.") },
        { "ranking.wave", ("Oleada {0}", "Wave {0}") },
    };

    // Mensajes que el backend devuelve en español y que el cliente enseña al jugador.
    private static readonly Dictionary<string, string> ServerMessages = new()
    {
        { "El username es obligatorio.", "server.usernameRequired" },
        { "El email es obligatorio.", "server.emailRequired" },
        { "La contraseña es obligatoria.", "server.passwordRequired" },
        { "El nombre de usuario debe tener entre 3 y 16 caracteres: letras, números o _.", "msg.usernameRule" },
        { "Ese email ya está registrado.", "server.emailTaken" },
        { "Ese username ya está en uso.", "server.usernameTaken" },
        { "Usuario registrado correctamente.", "server.registered" },
        { "El email o usuario es obligatorio.", "server.loginRequired" },
        { "Credenciales incorrectas.", "server.badCredentials" },
        { "Login correcto.", "server.loginOk" },
        { "El jugador no está conectado.", "server.playerOffline" },
    };

    public static IEnumerable<string> Keys => Texts.Keys;

    public static bool TryGet(string key, LanguageManager.Language language, out string text)
    {
        if (key != null && Texts.TryGetValue(key, out var pair))
        {
            text = language == LanguageManager.Language.Spanish ? pair.es : pair.en;
            return true;
        }
        text = null;
        return false;
    }

    public static bool TryGetServerKey(string message, out string key)
    {
        return ServerMessages.TryGetValue(message, out key);
    }
}
