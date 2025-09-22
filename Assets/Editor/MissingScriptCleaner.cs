#if UNITY_EDITOR
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class MissingScriptCleaner
{
    // --- CURRENT SCENE ONLY ---
    [MenuItem("Tools/Cleanup/Remove Missing Scripts (Current Scene)")]
    public static void RemoveMissing_CurrentScene()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

        int totalRemoved = 0;
        var scene = SceneManager.GetActiveScene();
        var roots = scene.GetRootGameObjects();
        for (int i = 0; i < roots.Length; i++)
        {
            EditorUtility.DisplayProgressBar("Cleaning Missing Scripts (Scene)",
                roots[i].name, (float)i / Mathf.Max(1, roots.Length - 1));
            totalRemoved += CleanHierarchy(roots[i]);
        }
        EditorUtility.ClearProgressBar();

        Debug.Log($"[MissingScriptCleaner] Removed {totalRemoved} missing script components from scene '{scene.name}'.");
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
    }

    // --- SCENE + PREFABS (WHOLE PROJECT) ---
    [MenuItem("Tools/Cleanup/Remove Missing Scripts (Scene + Prefabs)")]
    public static void RemoveMissing_SceneAndPrefabs()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

        int totalRemoved = 0;

        // 1) Current scene
        var scene = SceneManager.GetActiveScene();
        var roots = scene.GetRootGameObjects();
        for (int i = 0; i < roots.Length; i++)
        {
            EditorUtility.DisplayProgressBar("Cleaning Missing Scripts (Scene)",
                roots[i].name, (float)i / Mathf.Max(1, roots.Length - 1));
            totalRemoved += CleanHierarchy(roots[i]);
        }
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);

        // 2) All prefabs in project
        string[] prefabGuids = AssetDatabase.FindAssets("t:Prefab");
        for (int i = 0; i < prefabGuids.Length; i++)
        {
            string path = AssetDatabase.GUIDToAssetPath(prefabGuids[i]);
            EditorUtility.DisplayProgressBar("Cleaning Missing Scripts (Prefabs)", path, (float)i / Mathf.Max(1, prefabGuids.Length - 1));

            var go = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (!go) continue;

            int before = CountMissing(go);
            if (before > 0)
            {
                // Open prefab contents, clean, then save
                var stage = PrefabUtility.LoadPrefabContents(path);
                int removed = CleanHierarchy(stage);
                totalRemoved += removed;
                PrefabUtility.SaveAsPrefabAsset(stage, path);
                PrefabUtility.UnloadPrefabContents(stage);
            }
        }

        EditorUtility.ClearProgressBar();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"[MissingScriptCleaner] Removed {totalRemoved} missing script components from current scene and all prefabs.");
    }

    // ---- Helpers ----
    private static int CleanHierarchy(GameObject root)
    {
        int removed = 0;
        // Remove on root and all children
        removed += GameObjectUtility.RemoveMonoBehavioursWithMissingScript(root);
        foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
            removed += GameObjectUtility.RemoveMonoBehavioursWithMissingScript(t.gameObject);
        return removed;
    }

    private static int CountMissing(GameObject root)
    {
        int count = 0;
        // root
        count += root.GetComponents<MonoBehaviour>().Count(c => c == null);
        // children
        foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
            count += t.GetComponents<MonoBehaviour>().Count(c => c == null);
        return count;
    }
}
#endif
