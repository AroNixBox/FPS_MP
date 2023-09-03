using System;
using System.Collections;
using System.Collections.Generic;
using SpectrumConsole;
using UnityEngine;
using Unity.Netcode;
using UnityEngine.Rendering.UI;
using UnityEngine.SceneManagement;

public class Loader : NetworkBehaviour
{
    [SerializeField] private Transform playerPrefab;
    public static Loader Instance;
    [HideInInspector] public int expectedClientsCount;
    private int _connectedClientsCount;
    private string _currentPlayerNameLocalStorage;
    private string _currentPlayerIDLocalStorage;
    
    private Dictionary<ulong, PlayerData> playersData = new Dictionary<ulong, PlayerData>();
    private void Awake()
    {
        if (Instance == null)
        {
            //Because this is child of NetworkManager, this doesnt need DontDestroyOnLoad => When removing, need to add DDOL Here!
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
        int index = 0;

        foreach (var clientID in NetworkManager.Singleton.ConnectedClientsIds)
        {
            PlayerType selectedTeam;
            Transform selectedSpawnPoint;

            // Wähle Spieler Team aus
            if (index % 2 == 0)
            {
                selectedTeam = PlayerType.TeamBlue;
                selectedSpawnPoint = GetSpawnPointForTeam(selectedTeam);
            }
            else
            {
                selectedTeam = PlayerType.TeamRed;
                selectedSpawnPoint = GetSpawnPointForTeam(selectedTeam);
            }

            if (selectedSpawnPoint == null)
            {
                Debug.LogError($"Kein Spawnpunkt für Team {selectedTeam} gefunden!");
                continue;
            }
            UpdatePlayersAboutTeamSelectionServerRpc(clientID, selectedTeam);
            Transform playerTransform = Instantiate(playerPrefab, selectedSpawnPoint.position, selectedSpawnPoint.rotation);
            PlayerInfo playerInfo = playerTransform.GetComponent<PlayerInfo>();
            playerInfo.thisPlayersTeam = selectedTeam;
            

            playerTransform.GetComponent<NetworkObject>().SpawnAsPlayerObject(clientID, true);
            index++;
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void UpdatePlayersAboutTeamSelectionServerRpc(ulong clientID, PlayerType selectedTeam)
    {
        if (!IsServer) return;

        playersData[clientID].SelectedTeam = selectedTeam;

        UpdatePlayersAboutTeamSelectionClientRpc(clientID, selectedTeam);
    }

    [ClientRpc]
    private void UpdatePlayersAboutTeamSelectionClientRpc(ulong clientID, PlayerType selectedTeam)
    {
        playersData[clientID].SelectedTeam = selectedTeam;
    }

    private Transform GetSpawnPointForTeam(PlayerType team)
    {
        var spawnManager = FindObjectOfType<SpawnManager>();
        if (spawnManager != null)
        {
            return spawnManager.RequestSpawnPointForTeam(team);
        }

        return null;
    }

    public PlayerType GetPlayerTypeForClient(ulong clientID)
    {
        return playersData[clientID].SelectedTeam;
    }

    [Command]
    public PlayerData GetPlayerData()
    {
        //This Method returns the current PlayerEntry
        ulong localID = NetworkManager.Singleton.LocalClientId;
        Debug.Log($"PlayerID: {playersData[localID].PlayerID}, Name: {playersData[localID].PlayerName}, Kills: {playersData[localID].Kills}, Team: {playersData[localID].SelectedTeam}");
        return playersData.TryGetValue(NetworkManager.Singleton.LocalClientId, out var data) ? data : null;
    }
    [Command]
    private void PrintAllPlayerData()
    {
        foreach(var kvp in playersData)
        {
            print(expectedClientsCount);
            print(_connectedClientsCount);
            Debug.Log($"client ID: {kvp.Key}, PlayerID: {kvp.Value.PlayerID} Name: {kvp.Value.PlayerName}, Kills: {kvp.Value.Kills}, Deaths: {kvp.Value.Deaths}, Team: {kvp.Value.SelectedTeam}");
        }
    }
    public void SetCurrentPlayerAndSetParams(string playerId, string playerName)
    {
        _currentPlayerIDLocalStorage = playerId;
        _currentPlayerNameLocalStorage = playerName;
        StartCoroutine(WaitForEveryoneThenGo());
    }

    private IEnumerator WaitForEveryoneThenGo()
    {
        // TODO Find a better way to start the game.. Wait for everyone to connect then start, not some random Timer that runs down....
        yield return new WaitForSeconds(5);
        UpdatePlayerDataServerRpc(NetworkManager.Singleton.LocalClientId, _currentPlayerIDLocalStorage, _currentPlayerNameLocalStorage);
    }

    [ServerRpc(RequireOwnership = false)]
    private void UpdatePlayerDataServerRpc(ulong clientID, string playerId, string playerName)
    {
        if (!IsServer) return;
        
        if (!playersData.ContainsKey(clientID))
        {
            playersData[clientID] = new PlayerData();
            _connectedClientsCount++;
        }

        playersData[clientID].PlayerName = playerName;
        playersData[clientID].PlayerID = playerId;
        
        UpdateAllClientsAboutDataChangeClientRpc(clientID, playerId, playerName);
    }

    [ClientRpc]
    private void UpdateAllClientsAboutDataChangeClientRpc(ulong clientID, string playerId, string playerName)
    {
        if (!playersData.ContainsKey(clientID))
        {
            playersData[clientID] = new PlayerData();
        }

        playersData[clientID].PlayerName = playerName;
        playersData[clientID].PlayerID = playerId;

        if (_connectedClientsCount == expectedClientsCount)
        {
            ChangeScene();
            PrintAllPlayerData();
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void UpdatePlayerKillsServerRpc(ulong clientID)
    {
        if (playersData.ContainsKey(clientID))
        {
            playersData[clientID].Kills++;

            print($"{playersData[clientID].PlayerName} has {playersData[clientID].Kills} kills.");
            NotifyAllClientsAboutChangedKillsClientRpc(clientID, playersData[clientID].Kills);
        }
        else
        {
            Debug.LogWarning($"Client {clientID} not found in playersData dictionary.");
        }
    }
    [Command]
    private void GetSelectedTeam()
    {
        print("Players Team is: " + playersData[NetworkManager.Singleton.LocalClientId].SelectedTeam);
    }

    [ClientRpc]
    private void NotifyAllClientsAboutChangedKillsClientRpc(ulong clientID, int newKillCount)
    {
        if (playersData.ContainsKey(clientID))
        {
            playersData[clientID].Kills = newKillCount;
        }
        else
        {
            Debug.LogWarning($"Client {clientID} not found in playersData dictionary.");
        }
    }

    
    [ServerRpc(RequireOwnership = false)]
    public void UpdatePlayerDeathsServerRpc(ulong clientID)
    {
        if (playersData.ContainsKey(clientID))
        {
            playersData[clientID].Deaths++;

            print($"{playersData[clientID].PlayerName} has {playersData[clientID].Deaths} deaths.");
            NotifyAllClientsAboutChangedDeathsClientRpc(clientID, playersData[clientID].Deaths);
        }
        else
        {
            Debug.LogWarning($"Client {clientID} not found in playersData dictionary.");
        }
    }

    [ClientRpc]
    private void NotifyAllClientsAboutChangedDeathsClientRpc(ulong clientID, int newDeathCount)
    {
        if (playersData.ContainsKey(clientID))
        {
            playersData[clientID].Deaths = newDeathCount;
        }
        else
        {
            Debug.LogWarning($"Client {clientID} not found in playersData dictionary.");
        }
    }

    
    
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
    public int Deaths;
    public PlayerType SelectedTeam;

    public PlayerData()
    {
        PlayerName = "";
        Kills = 0;
        Deaths = 0;
    }

    public PlayerData(string name)
    {
        PlayerName = name;
        Kills = 0;
        Deaths = 0;
    }
}
