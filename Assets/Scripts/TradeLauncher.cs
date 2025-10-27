using System.Collections;
using System.Collections.Generic;
using Photon.Pun;
using Photon.Realtime;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TradeLauncher : MonoBehaviourPunCallbacks
{
    [Header("UI")]
    public TMP_Dropdown playerDropdown;
    public Button inviteButton;
    public TextMeshProUGUI statusText;   // optional

    // We’ll store ActorNumbers (always present in-room)
    private readonly List<int> _actorNumbers = new();

    void OnEnable()
    {
        if (inviteButton) inviteButton.onClick.AddListener(OnInviteClicked);
        StartCoroutine(RefreshSoon()); // refresh after a short delay to allow join flow to settle
    }

    void OnDisable()
    {
        if (inviteButton) inviteButton.onClick.RemoveListener(OnInviteClicked);
    }

    IEnumerator RefreshSoon()
    {
        // give Photon a frame or two to populate PlayerList after scene load
        yield return null;
        yield return null;
        RefreshList();
    }

    private void SetUIState(int optionsCount, string extra = "")
    {
        if (playerDropdown) playerDropdown.interactable = optionsCount > 0;
        if (inviteButton) inviteButton.interactable = optionsCount > 0;

        if (statusText)
        {
            var room = PhotonNetwork.CurrentRoom;
            string roomName = room != null ? room.Name : "(no room)";
            string region = PhotonNetwork.CloudRegion ?? "(no region)";
            statusText.text = (optionsCount > 0)
                ? $"Pick a player to trade  • Room: {roomName}  • Region: {region} {extra}"
                : $"No other players in room  • Room: {roomName}  • Region: {region} {extra}";
        }
    }

    private void RefreshList()
    {
        _actorNumbers.Clear();
        if (playerDropdown) playerDropdown.ClearOptions();

        var options = new List<TMP_Dropdown.OptionData>();

        // Use the room’s dictionary to avoid any edge cases
        var room = PhotonNetwork.CurrentRoom;
        if (room != null && room.Players != null)
        {
            foreach (var kv in room.Players)
            {
                var p = kv.Value;
                if (p == null) continue;
                if (p.IsLocal) continue; // skip self

                _actorNumbers.Add(p.ActorNumber);
                string label = string.IsNullOrEmpty(p.NickName)
                    ? $"Player #{p.ActorNumber}"
                    : $"{p.NickName} (#{p.ActorNumber})";
                options.Add(new TMP_Dropdown.OptionData(label));
            }
        }

        if (playerDropdown) playerDropdown.AddOptions(options);
        SetUIState(options.Count, $" • Players seen: {PhotonNetwork.PlayerList.Length}");
    }

    private void OnInviteClicked()
    {
        if (_actorNumbers.Count == 0) { SetUIState(0, " • No selection"); return; }

        int idx = playerDropdown ? playerDropdown.value : 0;
        if (idx < 0 || idx >= _actorNumbers.Count) { SetUIState(0, " • Invalid index"); return; }

        TradeManager.Instance.InviteByActor(_actorNumbers[idx]);
        if (statusText) statusText.text = "Invite sent!";
    }

    // Keep list fresh on roster changes
    public override void OnJoinedRoom()                   => RefreshList();
    public override void OnPlayerEnteredRoom(Player _)    => RefreshList();
    public override void OnPlayerLeftRoom(Player _)       => RefreshList();
}
