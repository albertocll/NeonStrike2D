#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

public static class ClearPlayerAnimEvents
{
    private const string PATH = "Assets/Art/Animations";

    [MenuItem("Tools/Animations/Clear Events in Assets/Art/Animations")]
    public static void Clear()
    {
        var guids = AssetDatabase.FindAssets("t:AnimationClip", new[] { PATH });

        int clips = 0, eventsRemoved = 0;

        foreach (var guid in guids)
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
            if (!clip) continue;

            var ev = AnimationUtility.GetAnimationEvents(clip);
            if (ev != null && ev.Length > 0)
            {
                eventsRemoved += ev.Length;
                AnimationUtility.SetAnimationEvents(clip, new AnimationEvent[0]);
                EditorUtility.SetDirty(clip);
                clips++;
            }
        }

        AssetDatabase.SaveAssets();
        Debug.Log($"Cleared events: {eventsRemoved} from clips: {clips} in {PATH}");
    }
}
#endif
