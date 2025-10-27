using System.Collections.Generic;
using UnityEngine;

/// Tracks picked up items during the current play session
public class SessionPickupTracker : MonoBehaviour
{
    private static SessionPickupTracker instance;
    public static SessionPickupTracker Instance
    {
        get
        {
            if (instance == null)
            {
                GameObject obj = new GameObject("SessionPickupTracker");
                instance = obj.AddComponent<SessionPickupTracker>();
                DontDestroyOnLoad(obj);
            }
            return instance;
        }
    }

    // Runtime tracking (for current session)
    private HashSet<string> pickedUpItemsThisSession = new HashSet<string>();

    void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
            Debug.Log("[SessionPickupTracker] Initialized for this play session");
        }
        else if (instance != this)
        {
            Destroy(gameObject);
        }
    }

    /// Check if an item was picked up this session (or in previous sessions for builds)
    public bool WasPickedUp(string pickupKey)
    {
#if UNITY_EDITOR
        // In editor: Only check runtime memory (resets each play session)
        bool result = pickedUpItemsThisSession.Contains(pickupKey);
        Debug.Log($"[SessionPickupTracker] EDITOR: Check '{pickupKey}' = {result} (Total tracked: {pickedUpItemsThisSession.Count})");

        // Debug: Show all tracked pickups
        if (pickedUpItemsThisSession.Count > 0)
        {
            Debug.Log($"[SessionPickupTracker] Currently tracked pickups: {string.Join(", ", pickedUpItemsThisSession)}");
        }

        return result;
#else
        // In build: Check PlayerPrefs (persists forever)
        return PlayerPrefs.GetInt(pickupKey, 0) == 1;
#endif
    }

    /// Mark an item as picked up
    public void MarkAsPickedUp(string pickupKey)
    {
#if UNITY_EDITOR
        // In editor: Store in runtime memory only
        pickedUpItemsThisSession.Add(pickupKey);
        Debug.Log($"[SessionPickupTracker] EDITOR: Marked '{pickupKey}' as picked up (session only)");
        Debug.Log($"[SessionPickupTracker] Total pickups this session: {pickedUpItemsThisSession.Count}");
#else
        // In build: Save to PlayerPrefs permanently
        PlayerPrefs.SetInt(pickupKey, 1);
        PlayerPrefs.Save();
        Debug.Log($"[SessionPickupTracker] BUILD: Saved '{pickupKey}' to PlayerPrefs (permanent)");
#endif
    }

    /// Clear a specific pickup (for testing)
    public void ClearPickup(string pickupKey)
    {
#if UNITY_EDITOR
        pickedUpItemsThisSession.Remove(pickupKey);
        Debug.Log($"[SessionPickupTracker] EDITOR: Cleared '{pickupKey}' from session");
#else
        PlayerPrefs.DeleteKey(pickupKey);
        PlayerPrefs.Save();
#endif
    }

    /// Clear all pickups (for testing)
    public void ClearAllPickups()
    {
#if UNITY_EDITOR
        pickedUpItemsThisSession.Clear();
        Debug.Log("[SessionPickupTracker] EDITOR: Cleared all pickups from session");
#else
        PlayerPrefs.DeleteAll();
        PlayerPrefs.Save();
#endif
    }
}