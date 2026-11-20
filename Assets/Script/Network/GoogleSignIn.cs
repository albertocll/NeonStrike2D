using System.Threading.Tasks;
using UnityEngine;

// Inicio de sesión con Google en Android con Credential Manager (issue #75).
// Pide al sistema un id_token de Google para el ID de cliente web; el token se manda tal cual
// al backend (/auth/google), que es quien lo verifica. Fuera de Android no está disponible.
public static class GoogleSignIn
{
    public static bool IsSupported => Application.platform == RuntimePlatform.Android;

    // Devuelve el id_token, o null con el motivo en error si el usuario cancela o algo falla.
    public static Task<(string idToken, string error)> GetIdTokenAsync(string webClientId)
    {
        var tcs = new TaskCompletionSource<(string, string)>(TaskCreationOptions.RunContinuationsAsynchronously);

        if (!IsSupported)
        {
            tcs.SetResult((null, "El inicio de sesión con Google solo está disponible en Android."));
            return tcs.Task;
        }

        if (string.IsNullOrWhiteSpace(webClientId))
        {
            tcs.SetResult((null, "El inicio de sesión con Google no está configurado."));
            return tcs.Task;
        }

#if UNITY_ANDROID && !UNITY_EDITOR
        try
        {
            using var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
            var activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
            using var plugin = new AndroidJavaClass("com.albertocll.neonstrike2d.GoogleSignInPlugin");
            plugin.CallStatic("signIn", activity, webClientId, new Callback(tcs));
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[GoogleSignIn] No se pudo llamar al plugin: {e}");
            tcs.TrySetResult((null, "No se pudo abrir el inicio de sesión con Google."));
        }
#endif
        return tcs.Task;
    }

#if UNITY_ANDROID && !UNITY_EDITOR
    // Recibe la respuesta del plugin Java, que llega desde otro hilo; el await la devuelve al de Unity.
    private class Callback : AndroidJavaProxy
    {
        private readonly TaskCompletionSource<(string, string)> tcs;

        public Callback(TaskCompletionSource<(string, string)> tcs)
            : base("com.albertocll.neonstrike2d.GoogleSignInPlugin$Callback")
        {
            this.tcs = tcs;
        }

        public void onSuccess(string idToken) => tcs.TrySetResult((idToken, null));

        public void onError(string message)
        {
            Debug.LogWarning($"[GoogleSignIn] {message}");
            tcs.TrySetResult((null, "No se pudo iniciar sesión con Google."));
        }

        public void onCancel() => tcs.TrySetResult((null, "Inicio de sesión con Google cancelado."));
    }
#endif
}
