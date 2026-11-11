using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

// Build para Google Play (issue #276): AAB con IL2CPP y solo ARM64. La firma usa la keystore
// que haya en Player Settings > Publishing Settings; las contrasenas se leen de las variables
// de entorno NEONSTRIKE_KEYSTORE_PASS y NEONSTRIKE_KEY_PASS (nunca se guardan en el repo).
// Sin keystore, Unity firma con la clave de depuracion y Google Play rechaza la subida.
public static class BuildAndroidAab
{
    [MenuItem("NeonStrike2D/Build Android (AAB)")]
    public static void Build()
    {
        if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.Android &&
            !EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Android, BuildTarget.Android))
        {
            Debug.LogError("[BuildAndroidAab] No se pudo cambiar la plataforma a Android.");
            return;
        }

        PlayerSettings.SetScriptingBackend(UnityEditor.Build.NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
        PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
        EditorUserBuildSettings.buildAppBundle = true;

        bool signed = PlayerSettings.Android.useCustomKeystore && !string.IsNullOrEmpty(PlayerSettings.Android.keystoreName);
        if (signed)
        {
            PlayerSettings.Android.keystorePass = Environment.GetEnvironmentVariable("NEONSTRIKE_KEYSTORE_PASS") ?? "";
            PlayerSettings.Android.keyaliasPass = Environment.GetEnvironmentVariable("NEONSTRIKE_KEY_PASS") ?? "";
        }

        string path = Path.Combine("Builds", "Android", $"NeonStrike2D-{PlayerSettings.bundleVersion}.aab");
        Directory.CreateDirectory(Path.GetDirectoryName(path));

        var options = new BuildPlayerOptions
        {
            scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray(),
            locationPathName = path,
            target = BuildTarget.Android,
            options = BuildOptions.None,
        };

        BuildReport report = BuildPipeline.BuildPlayer(options);
        if (report.summary.result == BuildResult.Succeeded)
            Debug.Log($"[BuildAndroidAab] {path} ({report.summary.totalSize / (1024 * 1024)} MB){(signed ? "" : " SIN FIRMAR: falta la keystore de subida")}");
        else
            Debug.LogError($"[BuildAndroidAab] La build ha fallado: {report.summary.result}");
    }
}
