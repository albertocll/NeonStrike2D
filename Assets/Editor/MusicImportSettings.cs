using UnityEditor;
using UnityEngine;

// Ajustes de importación para las pistas nuevas de Assets/Music (issue #262): se reproducen en streaming y
// no se cargan hasta que suenan, así las 16 pistas no ocupan memoria a la vez ni dan tirones al entrar.
// Solo se aplica la primera vez que se importa cada archivo; lo que se cambie luego a mano se respeta.
public class MusicImportSettings : AssetPostprocessor
{
    private const string MusicFolder = "Assets/Music/";

    private void OnPreprocessAudio()
    {
        if (!assetPath.StartsWith(MusicFolder) || !assetImporter.importSettingsMissing) return;

        var importer = (AudioImporter)assetImporter;
        importer.loadInBackground = true;
        AudioImporterSampleSettings settings = importer.defaultSampleSettings;
        settings.loadType = AudioClipLoadType.Streaming;
        settings.preloadAudioData = false;
        settings.compressionFormat = AudioCompressionFormat.Vorbis;
        settings.quality = 0.65f;
        importer.defaultSampleSettings = settings;
    }
}
