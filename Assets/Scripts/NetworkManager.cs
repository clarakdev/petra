using Photon.Pun;
using Photon.Realtime;
using UnityEngine;

public class NetworkManager : MonoBehaviourPunCallbacks
{
    [Header("Config")]
    public int maxPlayers = 20;
    public static NetworkManager instance;   //singleton

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);

        PhotonNetwork.AutomaticallySyncScene = true;
    }

    private void Start()
    {
        //connecting to the master server at the beginning of the game
        PhotonNetwork.ConnectUsingSettings();
    }

    public override void OnConnectedToMaster()
    {
        PhotonNetwork.JoinLobby();
    }

    public override void OnJoinedLobby()
    {
        string sceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        string roomName = sceneName;   // "BattleRoom" or "SocialRoom"

        var options = new RoomOptions
        {
            MaxPlayers = (byte)maxPlayers,
            IsOpen = true,
            IsVisible = true
        };

        PhotonNetwork.JoinOrCreateRoom(roomName, options, TypedLobby.Default);
    }

    public override void OnJoinedRoom()
    {
        // Desired spawn position
        Vector3 spawnPosition = new Vector3(0f, -4.02f, 0f);

        // Instantiate player at spawnPosition
        GameObject playerObj = PhotonNetwork.Instantiate("Player", spawnPosition, Quaternion.identity);
        playerObj.name = "Player";

        // Instantiate PetSpawner for this player
        GameObject petSpawnerPrefab = Resources.Load<GameObject>("PetSpawner");
        if (petSpawnerPrefab != null && playerObj != null)
        {
            GameObject petSpawnerObj = Instantiate(petSpawnerPrefab, playerObj.transform.position, Quaternion.identity);
            petSpawnerObj.transform.SetParent(playerObj.transform);
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
