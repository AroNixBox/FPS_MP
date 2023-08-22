using TMPro;
using Unity.Services.Lobbies.Models;
using UnityEngine;
using UnityEngine;
using UnityEngine.UI;

public class SingleLobbyUI : MonoBehaviour
{

    [SerializeField] private Button button;
    [SerializeField] private TextMeshProUGUI lobbyNameText;
    [SerializeField] private TextMeshProUGUI maxPlayersText;
    [SerializeField] private TextMeshProUGUI gameModeText;
    
    private string lobbyId;

    private void Awake()
    {
        button.onClick.AddListener(OnClickJoinLobby);
    }

    public void Setup(Lobby lobby)
    {
        //Use LobbyIdLater on?
        lobbyId = lobby.Id;
        lobbyNameText.text = lobby.Name;
        maxPlayersText.text = lobby.MaxPlayers.ToString();
        gameModeText.text = lobby.Data["GameMode"].Value;
    }

    private void OnClickJoinLobby()
    {
        LobbyManager.Instance.JoinLobbyByClick(lobbyId);
    }
}

