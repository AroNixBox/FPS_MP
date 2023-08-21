using System;
using System.Linq;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class NetworkManagerUI : MonoBehaviour
{
    [SerializeField] private GameObject authentificationWindow;
    [SerializeField] private GameObject lobbyWindow;

    
    [Header("Buttons")]
    [SerializeField] private Button serverButton;
    [SerializeField] private Button hostButton;
    [SerializeField] private Button clientButton;
    [SerializeField] private Button createLobbyButton;
    [SerializeField] private Button quickJoinButton;
    [SerializeField] private Button leaveLobbyButton;
    [SerializeField] private Button authenticateButton;
    [SerializeField] private TextMeshProUGUI playerNameAuthenticate;

    private void Awake()
    {
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

}
