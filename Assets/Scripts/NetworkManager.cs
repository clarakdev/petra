using Photon.Pun;
using Photon.Realtime;
using UnityEditor.XR;
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


    [PunRPC]
    public void ChangeScene(string sceneName)
    {
        PhotonNetwork.LoadLevel(sceneName);
    }
}
