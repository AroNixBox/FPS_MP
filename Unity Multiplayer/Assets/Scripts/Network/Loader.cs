using System.Collections.Generic;
using SpectrumConsole;
using UnityEngine;
using Unity.Netcode;
using UnityEngine.SceneManagement;

public class Loader : NetworkBehaviour
{
    [SerializeField] private Transform playerPrefab;
    public static Loader Instance;
    [HideInInspector] public int expectedClientsCount;
    private int _connectedClientsCount;
    
    private Dictionary<string, PlayerData> playersData = new Dictionary<string, PlayerData>();
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(this.gameObject);
        }
    }

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            NetworkManager.Singleton.SceneManager.OnLoadEventCompleted += SceneManager_OnLoadEventComplete;
        }
    }

    private void SceneManager_OnLoadEventComplete(string scenename, LoadSceneMode loadscenemode, List<ulong> clientscompleted, List<ulong> clientstimedout)
    {
        foreach (var clientID in NetworkManager.Singleton.ConnectedClientsIds)
        {
            Transform playerTransform = Instantiate(playerPrefab);
            playerTransform.GetComponent<NetworkObject>().SpawnAsPlayerObject(clientID, true);
        }
    }
    
    public PlayerData GetPlayerData(string playerId)
    {
        return playersData.TryGetValue(playerId, out var data) ? data : null;
    }
    [Command]
    private void PrintAllPlayerData()
    {
        foreach(var kvp in playersData)
        {
            Debug.Log($"Player ID: {kvp.Key}, Name: {kvp.Value.PlayerName}, Kills: {kvp.Value.Kills}");
        }
    }

    public void WaitForClientToConnect(string playerId, string playerName, int kills)
    {
        // TODO add the clientID to the PlayerData, assign it here. Request from PlayerHealth When you die, assign the current players Death and the Shooters Kill!
        NetworkManager.Singleton.OnClientConnectedCallback += clientId => 
        {
            UpdatePlayerDataServerRpc(playerId, playerName, kills);
        };
    }

    [ServerRpc(RequireOwnership = false)]
    private void UpdatePlayerDataServerRpc(string playerId, string playerName, int kills)
    {
        if (!playersData.ContainsKey(playerId))
            playersData[playerId] = new PlayerData();

        playersData[playerId].PlayerName = playerName;
        playersData[playerId].Kills = kills;
        
        print($"{playersData[playerId].PlayerName} was added to Dict");

        // Informiere alle Clients über die Aktualisierung.
        UpdateAllClientsAboutDataChangeClientRpc(playerId, playerName, kills);
    }

    [ClientRpc]
    private void UpdateAllClientsAboutDataChangeClientRpc(string playerId, string playerName, int kills)
    {
        if (!playersData.ContainsKey(playerId))
            playersData[playerId] = new PlayerData();
        
        print($"{playersData[playerId].PlayerName} was added to  on ClientSync");

        playersData[playerId].PlayerName = playerName;
        playersData[playerId].Kills = kills;

        _connectedClientsCount++;
        if (_connectedClientsCount >= expectedClientsCount)
        {
            ChangeScene();
            PrintAllPlayerData();
        }
    }
    

    [Command]
    public void ChangeScene()
    {
        if(!IsServer) return;
        
        NetworkManager.Singleton.SceneManager.LoadScene("GameScene", LoadSceneMode.Single);
    }
}

[System.Serializable]
public class PlayerData
{
    public string PlayerName;
    public int Kills;

    public PlayerData()
    {
        PlayerName = "";
        Kills = 0;
    }

    public PlayerData(string name)
    {
        PlayerName = name;
        Kills = 0;
    }
}
