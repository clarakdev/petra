using UnityEditor;
using UnityEngine;
using UnityEditor.SceneManagement;

public static class CleanupMissingScripts
{
    [MenuItem("Tools/Cleanup/Remove Missing Scripts In Scene")]
    static void RemoveInScene()
    {
        int removed = 0;
        foreach (var go in Object.FindObjectsOfType<GameObject>(true))
            removed += GameObjectUtility.RemoveMonoBehavioursWithMissingScript(go);
        Debug.Log($"Removed {removed} missing script components from open scenes.");
        EditorSceneManager.MarkAllScenesDirty();
    }

    [MenuItem("Tools/Cleanup/Remove Missing Scripts In Project")]
    static void RemoveInProject()
    {
        var guids = AssetDatabase.FindAssets("t:Prefab t:GameObject");
        int total = 0;
        foreach (var guid in guids)
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            var go = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (!go) continue;
            int removed = GameObjectUtility.RemoveMonoBehavioursWithMissingScript(go);
            if (removed > 0) { total += removed; EditorUtility.SetDirty(go); }
        }
        AssetDatabase.SaveAssets();
        Debug.Log($"Removed {total} missing script components from project assets.");
    }
}
