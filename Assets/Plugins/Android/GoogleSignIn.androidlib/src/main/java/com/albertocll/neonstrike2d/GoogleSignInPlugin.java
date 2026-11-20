package com.albertocll.neonstrike2d;

import android.app.Activity;
import android.os.CancellationSignal;

import androidx.credentials.Credential;
import androidx.credentials.CredentialManager;
import androidx.credentials.CredentialManagerCallback;
import androidx.credentials.CustomCredential;
import androidx.credentials.GetCredentialRequest;
import androidx.credentials.GetCredentialResponse;
import androidx.credentials.exceptions.GetCredentialCancellationException;
import androidx.credentials.exceptions.GetCredentialException;

import com.google.android.libraries.identity.googleid.GetSignInWithGoogleOption;
import com.google.android.libraries.identity.googleid.GoogleIdTokenCredential;

import java.util.concurrent.Executors;

// Pide a Credential Manager un id_token de Google para el ID de cliente web (issue #75).
// Lo llama GoogleSignIn.cs desde Unity; el token se verifica en el backend, no aquí.
public final class GoogleSignInPlugin {

    public interface Callback {
        void onSuccess(String idToken);
        void onError(String message);
        void onCancel();
    }

    private GoogleSignInPlugin() { }

    public static void signIn(final Activity activity, final String webClientId, final Callback callback) {
        GetSignInWithGoogleOption option = new GetSignInWithGoogleOption.Builder(webClientId).build();
        GetCredentialRequest request = new GetCredentialRequest.Builder()
                .addCredentialOption(option)
                .build();

        CredentialManager manager = CredentialManager.create(activity);
        manager.getCredentialAsync(
                activity,
                request,
                new CancellationSignal(),
                Executors.newSingleThreadExecutor(),
                new CredentialManagerCallback<GetCredentialResponse, GetCredentialException>() {
                    @Override
                    public void onResult(GetCredentialResponse response) {
                        Credential credential = response.getCredential();
                        if (credential instanceof CustomCredential
                                && GoogleIdTokenCredential.TYPE_GOOGLE_ID_TOKEN_CREDENTIAL.equals(credential.getType())) {
                            try {
                                GoogleIdTokenCredential google = GoogleIdTokenCredential.createFrom(credential.getData());
                                callback.onSuccess(google.getIdToken());
                            } catch (Exception e) {
                                callback.onError("Respuesta de Google no válida: " + e.getMessage());
                            }
                        } else {
                            callback.onError("Tipo de credencial inesperado: " + credential.getType());
                        }
                    }

                    @Override
                    public void onError(GetCredentialException e) {
                        if (e instanceof GetCredentialCancellationException) {
                            callback.onCancel();
                        } else {
                            callback.onError(e.getClass().getSimpleName() + ": " + e.getMessage());
                        }
                    }
                });
    }
}
