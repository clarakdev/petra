using Photon.Pun;
using Photon.Realtime;
using UnityEngine;

public class NetworkManager : MonoBehaviourPunCallbacks
{
    [Header("Config")]
    public int maxPlayers = 20;
    public static NetworkManager instance;   // Singleton instance

    private void Awake()
    {
        // Ensure only one instance of NetworkManager exists
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);

        // Sync scene loading across clients
        PhotonNetwork.AutomaticallySyncScene = true;
    }

    private void Start()
    {
        // Connect to Photon master server at game start
        PhotonNetwork.ConnectUsingSettings();
    }

    public override void OnConnectedToMaster()
    {
        // Join the default lobby after connecting
        PhotonNetwork.JoinLobby();
    }

    public override void OnJoinedLobby()
    {
        // Use the current scene name as the room name
        string sceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        string roomName = sceneName; // Example: "BattleRoom" or "SocialRoom"

        // Define room settings
        var options = new RoomOptions
        {
            MaxPlayers = (byte)maxPlayers,
            IsOpen = true,
            IsVisible = true,
            PublishUserId = true // Ensure Player.UserId is available in the room
        };

        // Try to join or create a room with this name
        PhotonNetwork.JoinOrCreateRoom(roomName, options, TypedLobby.Default);
    }

    public override void OnJoinedRoom()
    {
        string sceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;

        // Only spawn player and pet follower in non-battle scenes
        if (sceneName != "Battlefield")
        {
            // Define the spawn position
            Vector3 spawnPosition = new Vector3(0f, -4.02f, 0f);

            // Instantiate the player object
            GameObject playerObj = PhotonNetwork.Instantiate("Player", spawnPosition, Quaternion.identity);
            playerObj.name = "Player";

            // Instantiate PetSpawner and attach it to the player
            GameObject petSpawnerPrefab = Resources.Load<GameObject>("PetSpawner");
            if (petSpawnerPrefab != null && playerObj != null)
            {
                GameObject petSpawnerObj = Instantiate(petSpawnerPrefab, playerObj.transform.position, Quaternion.identity);
                petSpawnerObj.transform.SetParent(playerObj.transform);
            }
        }
    }

    public override void OnJoinRoomFailed(short returnCode, string message)
    {
        Debug.LogError($"JoinRoomFailed: {message}");
    }

    public override void OnCreateRoomFailed(short returnCode, string message)
    {
        Debug.LogError($"CreateRoomFailed: {message}");
    }

    [PunRPC]
    public void ChangeScene(string sceneName)
    {
        PhotonNetwork.LoadLevel(sceneName);
    }
}
