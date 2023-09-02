using System;
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
    
    private Dictionary<ulong, PlayerData> playersData = new Dictionary<ulong, PlayerData>();
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
    [Command]
    public PlayerData GetPlayerData()
    {
        //This Method returns the current PlayerEntry
        ulong localID = NetworkManager.Singleton.LocalClientId;
        Debug.Log($"PlayerID: {playersData[localID].PlayerID}, Name: {playersData[localID].PlayerName}, Kills: {playersData[localID].Kills}");
        return playersData.TryGetValue(NetworkManager.Singleton.LocalClientId, out var data) ? data : null;
    }
    [Command]
    private void PrintAllPlayerData()
    {
        foreach(var kvp in playersData)
        {
            Debug.Log($"client ID: {kvp.Key}, PlayerID: {kvp.Value.PlayerID} Name: {kvp.Value.PlayerName}, Kills: {kvp.Value.Kills}");
        }
    }

    public void WaitForClientToConnect(string playerId, string playerName, int kills)
    {
        //This clientID is not to use!!! Its giving always the value 1!!!!
        NetworkManager.Singleton.OnClientConnectedCallback += clientId => 
        {
            UpdatePlayerDataServerRpc(NetworkManager.Singleton.LocalClientId, playerId, playerName, kills);
        };
    }

    [ServerRpc(RequireOwnership = false)]
    private void UpdatePlayerDataServerRpc(ulong clientID, string playerId, string playerName, int kills)
    {
        if (!playersData.ContainsKey(clientID))
            playersData[clientID] = new PlayerData();

        playersData[clientID].PlayerName = playerName;
        playersData[clientID].Kills = kills;
        playersData[clientID].PlayerID = playerId;
        
        print($"{playersData[clientID].PlayerName} was added to Dict");

        // Informiere alle Clients über die Aktualisierung.
        UpdateAllClientsAboutDataChangeClientRpc(clientID, playerId, playerName, kills);
    }

    [ClientRpc]
    private void UpdateAllClientsAboutDataChangeClientRpc(ulong clientID, string playerId, string playerName, int kills)
    {
        if (!playersData.ContainsKey(clientID))
            playersData[clientID] = new PlayerData();
        
        print($"{playersData[clientID].PlayerName} was added to  on ClientSync");

        playersData[clientID].PlayerName = playerName;
        playersData[clientID].Kills = kills;
        playersData[clientID].PlayerID = playerId;

        _connectedClientsCount++;
        if (_connectedClientsCount >= expectedClientsCount)
        {
            ChangeScene();
            PrintAllPlayerData();
        }
    }


    [Command]
    private void ChangeScene()
    {
        if(!IsServer) return;
        
        NetworkManager.Singleton.SceneManager.LoadScene("GameScene", LoadSceneMode.Single);
    }
}

[System.Serializable]
public class PlayerData
{
    //Callable via the ClientID generated when loading the scene.
    // TODO Check if ClientID changes on ReloadScene?
    public string PlayerName;
    public string PlayerID;
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
