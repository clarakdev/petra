using UnityEngine;

public class FastTravelManager : MonoBehaviour
{
    [System.Serializable]
    public class TravelLocation
    {
        public string name;
        public Transform point;
    }

    public TravelLocation[] locations;
    public Transform player;
    public MapController mapController;

    public void TravelTo(string locationName)
    {
        foreach (var loc in locations)
        {
            if (loc.name == locationName && loc.point != null)
            {
                player.position = loc.point.position;

                // Resume gameplay and close map safely
                Time.timeScale = 1f;
                mapController?.CloseMap();

                Debug.Log("Teleported to: " + locationName);
                return;
            }
        }

        Debug.LogWarning("Travel location not found: " + locationName);
    }
}
