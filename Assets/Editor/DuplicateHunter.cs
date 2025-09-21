#if UNITY_EDITOR
using System.Linq;
using UnityEditor;
using UnityEngine;

public static class DuplicateHunter
{
    [MenuItem("Tools/Cleanup/Find 'PlaySatisfaction' Scripts")]
    public static void FindPlaySatisfaction()
    {
        var guids = AssetDatabase.FindAssets("t:MonoScript PlaySatisfaction");
        var hits = guids
            .Select(g => AssetDatabase.GUIDToAssetPath(g))
            .Select(p => new { path = p, script = AssetDatabase.LoadAssetAtPath<MonoScript>(p) })
            .Where(x => x.script != null && x.script.GetClass() != null && x.script.GetClass().Name == "PlaySatisfaction")
            .ToList();

        if (hits.Count <= 1)
        {
            EditorUtility.DisplayDialog("Duplicate Hunter", hits.Count == 1
                ? "Exactly one PlaySatisfaction found. You're good."
                : "No PlaySatisfaction class found.", "OK");
            if (hits.Count == 1) Selection.activeObject = hits[0].script;
            return;
        }

        var msg = "Multiple files define class PlaySatisfaction:\n\n" +
                  string.Join("\n", hits.Select(h => h.path)) +
                  "\n\nKeep ONE and delete the rest (and their .meta).";
        Debug.LogError(msg);
        Selection.objects = hits.Select(h => h.script).ToArray();
        EditorUtility.DisplayDialog("Duplicate Hunter", msg, "OK");
    }
}
#endif
