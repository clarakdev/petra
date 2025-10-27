using System.IO;
using UnityEngine;

public static class SaveSystem
{
    private const string FileName = "savegame.json";

    private static string FullPath =>
        Path.Combine(Application.persistentDataPath, FileName);

    public static void Save(SaveData data)
    {
        try
        {
            string json = JsonUtility.ToJson(data, prettyPrint: true);
            File.WriteAllText(FullPath, json);

#if UNITY_EDITOR
            Debug.Log($"[SaveSystem] Saved to: {FullPath}\n{json}");
#endif
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[SaveSystem] Save failed: {ex}");
        }
    }

    public static SaveData Load()
    {
        try
        {
            if (!File.Exists(FullPath))
            {
                Debug.Log("[SaveSystem] No save file found.");
                return null;
            }

            string json = File.ReadAllText(FullPath);
            SaveData data = JsonUtility.FromJson<SaveData>(json);

#if UNITY_EDITOR
            Debug.Log($"[SaveSystem] Loaded from: {FullPath}\n{json}");
#endif
            return data;
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[SaveSystem] Load failed: {ex}");
            return null;
        }
    }

    public static void Delete()
    {
        try
        {
            if (File.Exists(FullPath))
            {
                File.Delete(FullPath);
                Debug.Log("[SaveSystem] Save file deleted.");
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[SaveSystem] Delete failed: {ex}");
        }
    }
}
