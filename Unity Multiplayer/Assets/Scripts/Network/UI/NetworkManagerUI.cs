using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using Unity.Netcode;
using Unity.Services.Lobbies.Models;
using UnityEngine;
using UnityEngine.UI;

public class NetworkManagerUI : MonoBehaviour
{
    public static NetworkManagerUI Instance;
    [Header("Windows")]
    [SerializeField] private GameObject authentificationWindow;
    [SerializeField] private GameObject lobbyWindow;
    [SerializeField] private GameObject joinedLobbyWindow;
    
    [Header("Lobbies")]
    [SerializeField] private Transform contentPanel;
    [SerializeField] private GameObject lobbyButtonPrefab;
    
    [Header("JoinedLobby")]
    [SerializeField] private TextMeshProUGUI playerNamePrefab;
    [SerializeField] private Transform playersJoinedLobbyContentParent;
    [SerializeField] private List<TextMeshProUGUI> currentDisplayedNames = new List<TextMeshProUGUI>();

    [Header("Buttons")]
    [SerializeField] private Button serverButton;
    [SerializeField] private Button hostButton;
    [SerializeField] private Button clientButton;
    [SerializeField] private Button createLobbyButton;
    [SerializeField] private Button quickJoinButton;
    [SerializeField] private Button leaveLobbyButton;
    [SerializeField] private Button authenticateButton;
    [SerializeField] private Button startGameButton;
    [SerializeField] private TextMeshProUGUI playerNameAuthenticate;

    private void Awake()
    {
        if (Instance == null) 
        { 
            Instance = this; 
            DontDestroyOnLoad(gameObject);
        }
        else if (Instance != this) 
        { 
            Destroy(gameObject);
        }
        
        serverButton.onClick.AddListener(() =>
        {
            NetworkManager.Singleton.StartServer();
        });
        hostButton.onClick.AddListener(() =>
        {
            NetworkManager.Singleton.StartHost();
        });
        clientButton.onClick.AddListener(() =>
        {
            NetworkManager.Singleton.StartClient();
        });
        createLobbyButton.onClick.AddListener(() =>
        {
            LobbyManager.Instance.CreateLobby();
        });
        quickJoinButton.onClick.AddListener(() =>
        {
            LobbyManager.Instance.QuickJoinLobby();
        });
        leaveLobbyButton.onClick.AddListener(() =>
        {
            LobbyManager.Instance.LeaveLobby();
        });
        startGameButton.onClick.AddListener(() =>
        {
            LobbyManager.Instance.StartGame();
        });

        authenticateButton.onClick.AddListener(SendPlayerNameToLobby);

    }

    private void SendPlayerNameToLobby()
    {
        string playerName = playerNameAuthenticate.text;
        playerName = RemoveSpecialCharacters(playerName);
        
        if (IsAlphabetic(playerName) && playerName.Length >= 3)
        {
            LobbyManager.Instance.Authenticate(playerName);
            Debug.Log(playerName);
        }
        else
        {
            string emergencyName = "AnIdiotSandwich" + UnityEngine.Random.Range(1, 99).ToString();
            LobbyManager.Instance.Authenticate(emergencyName);
        }
        lobbyWindow.SetActive(true);
        authentificationWindow.SetActive(false);
    }
    private bool IsAlphabetic(string value)
    {
        foreach (char c in value)
        {
            if (!char.IsLetter(c))
            {
                return false;
            }
        }
        return true;
    }
    private string RemoveSpecialCharacters(string value)
    {
        return new string(value.Where(c => char.IsLetter(c) || char.IsWhiteSpace(c)).ToArray());
    }
    
    public void PrepareForLobbyUIRefresh()
    {
        // Entfernen Sie alle aktuellen Lobby-UI-Elemente, bevor Sie neue hinzufügen
        foreach (Transform child in contentPanel)
        {
            Destroy(child.gameObject);
        }
    }
    
    public void RefreshLobbiesUI(Lobby lobby)
    {
        GameObject newButton = Instantiate(lobbyButtonPrefab, contentPanel);
        SingleLobbyUI lobbyButton = newButton.GetComponent<SingleLobbyUI>();
        lobbyButton.Setup(lobby);
    }

    public void UpdateJoinedOrLeft(JoinedOrLeft state)
    {
        switch (state)
        {
            case JoinedOrLeft.Joined:
                joinedLobbyWindow.SetActive(true);
                lobbyWindow.SetActive(false);
                break;
            case JoinedOrLeft.Left:
                joinedLobbyWindow.SetActive(false);
                lobbyWindow.SetActive(true);
                break;
        }
    }
    
    public void UpdatePlayerNamesInLobby(List<Player> players)
    {
        //Destroy all recent names
        foreach (var textObj in currentDisplayedNames)
        {
            Destroy(textObj.gameObject);
        }
        currentDisplayedNames.Clear();

        //Create new tmp for each new joined player
        foreach (var player in players)
        {
            var joinedPlayerName = Instantiate(playerNamePrefab, playersJoinedLobbyContentParent);
            joinedPlayerName.text = player.Data["PlayerName"].Value;
            currentDisplayedNames.Add(joinedPlayerName);
        }
    }

    public void SetupGame()
    {
        joinedLobbyWindow.SetActive(false);
        lobbyWindow.SetActive(false);
    }
}
