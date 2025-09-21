#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class CleanupTools
{
    // ---------- REMOVE MISSING SCRIPTS ----------
    [MenuItem("Tools/Cleanup/Remove Missing Scripts (Active Scene)")]
    public static void RemoveMissingInActiveScene()
    {
        var scene = SceneManager.GetActiveScene();
        if (!scene.IsValid()) { Debug.LogWarning("No valid active scene."); return; }

        int removed = 0;
        foreach (var root in scene.GetRootGameObjects())
            removed += RemoveMissingInHierarchy(root);

        Debug.Log($"[Cleanup] Removed {removed} missing script components in active scene '{scene.name}'.");
        if (removed > 0) EditorSceneManager.MarkSceneDirty(scene);
    }

    [MenuItem("Tools/Cleanup/Remove Missing Scripts (All Open Scenes)")]
    public static void RemoveMissingInAllOpenScenes()
    {
        int total = 0;
        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            var scene = SceneManager.GetSceneAt(i);
            int removed = 0;
            foreach (var root in scene.GetRootGameObjects())
                removed += RemoveMissingInHierarchy(root);

            total += removed;
            if (removed > 0) EditorSceneManager.MarkSceneDirty(scene);
            Debug.Log($"[Cleanup] Scene '{scene.name}': removed {removed} missing script components.");
        }
        Debug.Log($"[Cleanup] Total removed in all open scenes: {total}");
    }

    [MenuItem("Tools/Cleanup/Remove Missing Scripts (All Prefabs in Assets)")]
    public static void RemoveMissingInAllPrefabs()
    {
        string[] prefabGuids = AssetDatabase.FindAssets("t:Prefab");
        int total = 0;

        for (int i = 0; i < prefabGuids.Length; i++)
        {
            string path = AssetDatabase.GUIDToAssetPath(prefabGuids[i]);
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null) continue;

            int removed = RemoveMissingInPrefab(prefab);
            total += removed;
            if (removed > 0)
            {
                Debug.Log($"[Cleanup] Prefab '{path}': removed {removed} missing script components.");
            }
        }

        AssetDatabase.SaveAssets();
        Debug.Log($"[Cleanup] Total removed in prefabs: {total}");
    }

    static int RemoveMissingInHierarchy(GameObject root)
    {
        int removed = 0;
        var stack = new Stack<Transform>();
        stack.Push(root.transform);

        while (stack.Count > 0)
        {
            var t = stack.Pop();
            removed += GameObjectUtility.RemoveMonoBehavioursWithMissingScript(t.gameObject);
            for (int i = 0; i < t.childCount; i++)
                stack.Push(t.GetChild(i));
        }
        return removed;
    }

    static int RemoveMissingInPrefab(GameObject prefab)
    {
        int removed = 0;
        // Open in isolation so we can modify safely
        var stage = UnityEditor.SceneManagement.PrefabStageUtility.GetPrefabStage(prefab);
        if (stage != null)
        {
            // Already open in prefab stage
            removed += RemoveMissingInHierarchy(stage.prefabContentsRoot);
        }
        else
        {
            var path = AssetDatabase.GetAssetPath(prefab);
            var contents = PrefabUtility.LoadPrefabContents(path);
            removed += RemoveMissingInHierarchy(contents);
            if (removed > 0)
            {
                PrefabUtility.SaveAsPrefabAsset(contents, path);
            }
            PrefabUtility.UnloadPrefabContents(contents);
        }
        return removed;
    }

    // ---------- REBUILD BUILD SETTINGS ----------
    [MenuItem("Tools/Cleanup/Rebuild Build Settings From Assets/Scenes")]
    public static void RebuildBuildSettings()
    {
        string scenesFolder = "Assets/Scenes";
        if (!AssetDatabase.IsValidFolder(scenesFolder))
        {
            EditorUtility.DisplayDialog("Rebuild Build Settings",
                $"Folder '{scenesFolder}' was not found.\nCreate it or change the code to your scenes path.",
                "OK");
            return;
        }

        string[] sceneGuids = AssetDatabase.FindAssets("t:Scene", new[] { scenesFolder });
        var entries = sceneGuids
            .Select(g => AssetDatabase.GUIDToAssetPath(g))
            .OrderBy(p => p)
            .Select(p => new EditorBuildSettingsScene(p, true))
            .ToArray();

        EditorBuildSettings.scenes = entries;
        Debug.Log($"[Cleanup] Rebuilt Build Settings with {entries.Length} scenes from '{scenesFolder}'.");
    }

    // ---------- REPORT DUPLICATE CLASS NAMES ----------
    [MenuItem("Tools/Cleanup/Report Duplicate Class Names (C#)")]
    public static void ReportDuplicateClassNames()
    {
        var classToFiles = new Dictionary<string, List<string>>();
        var csGuids = AssetDatabase.FindAssets("t:TextAsset", new[] { "Assets" })
                                   .Where(g => AssetDatabase.GUIDToAssetPath(g).EndsWith(".cs"));

        var classRegex = new Regex(@"\b(class|struct)\s+([A-Za-z_][A-Za-z0-9_]*)\b",
                                   RegexOptions.Multiline);

        foreach (var guid in csGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            string text = File.ReadAllText(path);

            foreach (Match m in classRegex.Matches(text))
            {
                string name = m.Groups[2].Value;
                if (!classToFiles.TryGetValue(name, out var list))
                {
                    list = new List<string>();
                    classToFiles[name] = list;
                }
                list.Add(path);
            }
        }

        var dupes = classToFiles.Where(kvp => kvp.Value.Count > 1).ToList();
        if (dupes.Count == 0)
        {
            Debug.Log("[Cleanup] No duplicate class/struct names found in Assets.");
            return;
        }

        foreach (var kvp in dupes)
        {
            string joined = string.Join("\n  • ", kvp.Value);
            Debug.LogWarning($"[Cleanup] Duplicate type '{kvp.Key}' found in:\n  • {joined}");
        }

        // Also select the files of the FIRST duplicate to help you jump there
        var first = dupes.First();
        var objs = first.Value.Select(p => AssetDatabase.LoadMainAssetAtPath(p)).ToArray();
        Selection.objects = objs.Where(o => o != null).ToArray();
    }

    // ---------- QUICK HELP ----------
    [MenuItem("Tools/Cleanup/Help")]
    public static void Help()
    {
        EditorUtility.DisplayDialog(
            "Cleanup Tools",
            "1) Run Remove Missing Scripts (Scenes + Prefabs)\n" +
            "2) Run Report Duplicate Class Names and delete/rename dupes (e.g., extra PlaySatisfaction files)\n" +
            "3) Rebuild Build Settings (Assets/Scenes)\n" +
            "If Library/Unused keeps throwing PPtr errors, close Unity and delete the 'Library' and 'Temp' folders, then reopen.",
            "OK");
    }
}
#endif
