using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Lobbies;
using UnityEngine;
using Unity.Services.Lobbies.Models;
using Unity.VisualScripting;

public enum JoinedOrLeft
{
    Joined,
    Left
}
public class LobbyManager : MonoBehaviour
{
    private Lobby _hostLobby;
    private Lobby _joinedLobby;
    private float _heartbeatTimer;
    private float _lobbyJoinedUpdateTimer;
    private float _lobbyUpdateTimer;
    private float _refreshLobbyListTimer;
    private string _playerName;
    //Has the player joined or left the player?
    
    
    //Saves all current lobbies to not destroy UI Every 5 Seconds, but only when changed.
    private Dictionary<string, Lobby> currentLobbies = new Dictionary<string, Lobby>();
    
    //Saves all current Players to not destroy every player each 5 seconds, but rather only when changed
    private List<string> previousPlayerIds = new List<string>();


    public static LobbyManager Instance;

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
    }
    private void Update()
    {
        //Prevent Lobby from being closed after 30 Seconds, Sends Heartbeat to Server
        HandleLobbyHeartbeat();
        HandleLobbyPollForUpdates();
        HandleRefreshLobbyList();
        HandleNamesListInLobby();
    }
    //Use this one for Button AUthentification
    public async void Authenticate(string playerName)
    {
        this._playerName = playerName;
        InitializationOptions initializationOptions = new InitializationOptions();
        initializationOptions.SetProfile(playerName);

        await UnityServices.InitializeAsync(initializationOptions);
        AuthenticationService.Instance.SignedIn += () =>
        {
            Debug.Log("Signed in! " + AuthenticationService.Instance.PlayerId + " " + GetPlayer().Data["PlayerName"].Value);
        };
        await AuthenticationService.Instance.SignInAnonymouslyAsync();
    }
    //Prevent Lobby from being closed after 30 Seconds, Sends Heartbeat to Server
    private async void HandleLobbyHeartbeat()
    {
        if (_hostLobby != null)
        {
            //Update Every 15 Seconds
            _heartbeatTimer -= Time.deltaTime;
            if (_heartbeatTimer < 0f)
            {
                float heartbeatTimerMax = 15f;
                _heartbeatTimer = heartbeatTimerMax;

                await LobbyService.Instance.SendHeartbeatPingAsync(_hostLobby.Id);
            }
        }
    }
    //Updates the Lobby each 1.1 Seconds to make sure if Gamemode/ Map is changed, this will be sent to the Server!
    //Also Handling to notice when leaving a lobby
    private async void HandleLobbyPollForUpdates()
    {
        //Can be called once per second
        if (_joinedLobby != null)
        {
            //Update Every 1.1 Seconds
            _lobbyUpdateTimer -= Time.deltaTime;
            if (_lobbyUpdateTimer < 0f)
            {
                float lobbyUpdateTimerMax = 1.1f;
                _lobbyUpdateTimer = lobbyUpdateTimerMax;

                Lobby lobby = await LobbyService.Instance.GetLobbyAsync(_joinedLobby.Id);
                _joinedLobby = lobby;

                if (_joinedLobby.Data["StartGame"].Value != "0")
                {
                    if (!IsLobbyHost())
                    {
                        RelayServer.Instance.JoinRelay(_joinedLobby.Data["StartGame"].Value);
                        NetworkManagerUI.Instance.SetupGame();
                    }

                    _joinedLobby = null;
                }

            }
        }
    }
    
    //Should have a Button
    public async void CreateLobby()
    {
        try
        {
            //Maybe Set LobbyName in a Field?
            string lobbyName = "MyLobby";
            //Maybe Set maxPlayers in a Field?
            int maxPlayers = 4;
            CreateLobbyOptions createLobbyOptions = new CreateLobbyOptions
            {
                //Change this to make it private or not
                IsPrivate = false,
                //When Lobby is created, Passing in the Creator, Defining Data, setting a Field as PlayerName and setting it to the actual PLayername
                Player  = GetPlayer(),
                
                //Sets the GameMode for the Created Lobby. Maybe Make it a Enum dropdown a Player can chose from. As Last parameter (value) the enum would be passed. Could Be TeamDeathMatch, 1v1 etc..
                Data = new Dictionary<string, DataObject>
                {
                    //This simply creates a Lobby as CaptureTheFlag
                    //VisibilityOptions.Public means readable for everyone, even if not joined to the Lobby. .Member means only readable for joined players.
                    {"GameMode", new DataObject(DataObject.VisibilityOptions.Public, "CaptureTheFlag")},
                    {"Map", new DataObject(DataObject.VisibilityOptions.Public, "DustyMountains")},
                    {"StartGame", new DataObject(DataObject.VisibilityOptions.Member, "0")}
                }
            };
            
            
            //lobbyName + maxPlayers should be set + the Info if the created lobby should be private or not
            Lobby lobby = await LobbyService.Instance.CreateLobbyAsync(lobbyName, maxPlayers, createLobbyOptions);
        
            //Important for Heartbeat
            _hostLobby = lobby;
            _joinedLobby = _hostLobby;
            
            PrintPlayers(_hostLobby);
            Debug.Log("Created Lobby: " + lobby.Name + " " + lobby.MaxPlayers + " " + lobby.Id + " " + lobby.LobbyCode);
            JoinedOrLeft currentState = JoinedOrLeft.Joined;
            NetworkManagerUI.Instance.UpdateJoinedOrLeft(currentState);
        }
        catch (LobbyServiceException e)
        {
            Debug.Log(e);
        }
        
    }
    private bool IsLobbyHost()
    {
        return _hostLobby != null;
    }
    //Autorefresh every 5 Seconds
    private void HandleRefreshLobbyList()
    {
        if (_joinedLobby != null)
            return;
        
        if (UnityServices.State == ServicesInitializationState.Initialized && AuthenticationService.Instance.IsSignedIn)
        {
            _refreshLobbyListTimer -= Time.deltaTime;
            if (_refreshLobbyListTimer < 0f)
            {
                float refreshLobbyListTimerMax = 5f;
                _refreshLobbyListTimer = refreshLobbyListTimerMax;

                RefreshLobbyList();
            }
        }
    }
    

    //If want to refresh Lobby by Button instead of refreshing every 5 Seconds, Isolate this function, dont call it above. Remove from Update and Link it to a button.
    private async void RefreshLobbyList()
    {
        try
        {
            QueryLobbiesOptions options = new QueryLobbiesOptions
            {
                Count = 25,
                //Filter for open lobbies only
                Filters = new List<QueryFilter>
                {
                    new QueryFilter(field: QueryFilter.FieldOptions.AvailableSlots,
                        op: QueryFilter.OpOptions.GT, value: "0")
                },
                Order = new List<QueryOrder>
                {
                    new QueryOrder(
                        asc: false,
                        field: QueryOrder.FieldOptions.Created)
                }
            };

            QueryResponse lobbyListQueryResponse = await Lobbies.Instance.QueryLobbiesAsync(options);
            
            List<Lobby> fetchedLobbies = lobbyListQueryResponse.Results;
            
            if (HasLobbyListChanged(fetchedLobbies))
            {
                Debug.Log("New Lobbies found: " + lobbyListQueryResponse.Results.Count);
                currentLobbies.Clear();

                NetworkManagerUI.Instance.PrepareForLobbyUIRefresh();
                foreach (Lobby lobby in fetchedLobbies)
                {

                    currentLobbies.Add(lobby.Id, lobby);
                    Debug.Log(lobby.Name + " " + lobby.MaxPlayers + " " + lobby.Data["GameMode"].Value);
                    // Update UI
                    NetworkManagerUI.Instance.RefreshLobbiesUI(lobby);
                }
            }
            Debug.Log("Lobbies found: " + lobbyListQueryResponse.Results.Count);
            foreach (Lobby lobby in lobbyListQueryResponse.Results)
            {
                //Show the Lobbies Name, its assigned MaxPlayers and the GameMode
                Debug.Log(lobby.Name + " " + lobby.MaxPlayers + " " + lobby.Data["GameMode"].Value);
            }
            
            //OnLobbyList
        }
        catch (LobbyServiceException e)
        {
            Console.WriteLine(e);
            throw;
        }
    }
    private bool HasLobbyListChanged(List<Lobby> newLobbies)
    {
        if (newLobbies.Count != currentLobbies.Count) return true;

        foreach (Lobby lobby in newLobbies)
        {
            if (!currentLobbies.ContainsKey(lobby.Id))
            {
                return true;
            }
        }

        return false;
    }
    private void HandleNamesListInLobby()
    {
        if (_joinedLobby == null)
            return;
        
        _lobbyJoinedUpdateTimer -= Time.deltaTime;
        if (_lobbyJoinedUpdateTimer < 0f)
        {
            float lobbyJoinedUpdateTimerMax = 5f;
            _lobbyJoinedUpdateTimer = lobbyJoinedUpdateTimerMax;
            if (HasPlayerListChanged(_joinedLobby.Players))
            {
                NetworkManagerUI.Instance.UpdatePlayerNamesInLobby(_joinedLobby.Players);
                previousPlayerIds = _joinedLobby.Players.Select(p => p.Id).ToList();
            }
        }
    }
    bool HasPlayerListChanged(List<Player> currentlyJoinedPlayers)
    {
        List<string> currentPlayerIds = currentlyJoinedPlayers.Select(p => p.Id).ToList();
        
        if (currentPlayerIds.Count != previousPlayerIds.Count)
        {
            return true;
        }

        for (int i = 0; i < currentPlayerIds.Count; i++)
        {
            if (currentPlayerIds[i] != previousPlayerIds[i])
            {
                return true;
            }
        }

        return false;
    }
    
    public async void JoinLobbyByClick(string lobbyID)
    {
        try
        {
            JoinLobbyByIdOptions joinLobbyByIdOptions = new JoinLobbyByIdOptions()
            {
                Player = GetPlayer()
            };

            //Tries to find the Lobby with the correct code, Code will be Set in the Lobbycreation if createLobbyOptions bool value is changed to true and set in the parameters
            Lobby lobby = await Lobbies.Instance.JoinLobbyByIdAsync(lobbyID, joinLobbyByIdOptions);
            _joinedLobby = lobby;
            Debug.Log("Click-Joined Lobby with ID: " + lobbyID);

            PrintPlayers(lobby);
            JoinedOrLeft currentState = JoinedOrLeft.Joined;
            NetworkManagerUI.Instance.UpdateJoinedOrLeft(currentState);
        }
        catch (LobbyServiceException e)
        {
            Console.WriteLine(e);
            throw;
        }
    }

    //Join the first Lobby that comes up 
    public async void QuickJoinLobby()
    {
        try
        {
            QuickJoinLobbyOptions quickJoinLobbyOptions = new QuickJoinLobbyOptions
            {
                Player = GetPlayer()
            };
            //Can Recieve Parameters, So could quickjoin a specific Maptype, Gamemode...
            Lobby lobby = await LobbyService.Instance.QuickJoinLobbyAsync(quickJoinLobbyOptions);
            _joinedLobby = lobby;
            PrintPlayers(lobby);
            JoinedOrLeft currentState = JoinedOrLeft.Joined;
            NetworkManagerUI.Instance.UpdateJoinedOrLeft(currentState);
        }
        catch (LobbyServiceException e)
        {
            Debug.Log(e);
        }

    }

    private Player GetPlayer()
    {
        return new Player
        {
            Data = new Dictionary<string, PlayerDataObject>
            {
                //String "PlayerName" is a Set Entry, should not be spelled different.
                { "PlayerName", new PlayerDataObject(PlayerDataObject.VisibilityOptions.Member, _playerName) }
            }
        };
    }

    //Shows wanted Info for all the Players joined. Could cycle through each player, get its Name, selected Icon, sort them in a UI-Grid
    private void PrintPlayers(Lobby lobby)
    {
        foreach (Player player in lobby.Players)
        {
            Debug.Log("Players in Lobby " + lobby.Name + " " + lobby.Data["GameMode"].Value + " " + lobby.Data["Map"].Value);
            //This is how to Access the Playernames Value of each joined Player + ID
            Debug.Log(player.Id + " " + player.Data["PlayerName"].Value);
            
        }
    }
    //Make this a button for the Host only
    public async void StartGame()
    {
        if (IsLobbyHost())
        {
            try
            {
                Debug.Log("Start Game");
                string relayCode = await RelayServer.Instance.CreateRelay();

                Lobby lobby = await Lobbies.Instance.UpdateLobbyAsync(_joinedLobby.Id, new UpdateLobbyOptions
                {
                    Data = new Dictionary<string, DataObject>
                    {
                        {"StartGame", new DataObject(DataObject.VisibilityOptions.Member, relayCode)}
                    }
                });
                _joinedLobby = lobby;
                NetworkManagerUI.Instance.SetupGame();
            }
            catch (LobbyServiceException e)
            {
                Debug.Log(e);
            }

        }

    }
    public async void LeaveLobby()
    {
        //>>>>>>>>>>>>>>Set After, if causes issues remove<<<<<<<<<
        if (_joinedLobby == null)
            return;
        
        try
        {
            JoinedOrLeft currentState = JoinedOrLeft.Left;
            NetworkManagerUI.Instance.UpdateJoinedOrLeft(currentState);
            await LobbyService.Instance.RemovePlayerAsync(_joinedLobby.Id, AuthenticationService.Instance.PlayerId);
            PrintJoinedLobby();
            //>>>>>>>>>>>>>>Set After, if causes issues remove<<<<<<<<<
            if (_hostLobby != null)
                _hostLobby = null;
            
            _joinedLobby = null;
        }
        catch (LobbyServiceException e)
        {
            Debug.Log(e);
        }

    }

    public async void JoinLobbyByCode(string lobbyCode)
    {
        try
        {
            JoinLobbyByCodeOptions joinLobbyByCodeOptions = new JoinLobbyByCodeOptions
            {
                Player = GetPlayer()
            };
            
            //Tries to find the Lobby with the correct code, Code will be Set in the Lobbycreation if createLobbyOptions bool value is changed to true and set in the parameters
            Lobby lobby = await Lobbies.Instance.JoinLobbyByCodeAsync(lobbyCode, joinLobbyByCodeOptions);
            _joinedLobby = lobby;
            Debug.Log("Joined Lobby with Code: " + lobbyCode);
         
            PrintPlayers(lobby);
            JoinedOrLeft currentState = JoinedOrLeft.Joined;
            NetworkManagerUI.Instance.UpdateJoinedOrLeft(currentState);
        }
        catch (LobbyServiceException e)
        {
            Console.WriteLine(e);
            throw;
        }
    }
    public void PrintJoinedLobby()
    {
        PrintPlayers(_joinedLobby);
    }

    //Call this Function when Host Updates the GameMode of the Lobby!
    //Create an Enum for all Gamemodes
    public async void UpdateLobbyGameMode(string updatedGameMode)
    {
        try
        {
            _hostLobby = await Lobbies.Instance.UpdateLobbyAsync(_hostLobby.Id, new UpdateLobbyOptions
            {
                Data = new Dictionary<string, DataObject>
                {
                    {
                        "GameMode", new DataObject(DataObject.VisibilityOptions.Public, updatedGameMode)
                        
                    }
                }
            });
            _joinedLobby = _hostLobby;
        }
        catch (LobbyServiceException e)
        {
            Debug.Log(e);
        }
    }
    //Call this Function when Host Updates the Map of the Lobby!
    //Create an Enum for all Gamemodes
    public async void UpdateLobbyMap(string updatedMap)
    {
        try
        {
            _hostLobby = await Lobbies.Instance.UpdateLobbyAsync(_hostLobby.Id, new UpdateLobbyOptions
            {
                Data = new Dictionary<string, DataObject>{
                    { "Map", new DataObject(DataObject.VisibilityOptions.Public, updatedMap)}
                }
            });
        }
        catch (LobbyServiceException e)
        {
            Debug.Log(e);
        }
    }
    
    //Updates Playerdata, so if Player Chose to change its Name/ Icon in the Lobby he can do that.
    //Maybe even Dont make the Name save, rather make it changeable in the lobby?
    //Random generation for Playername?
    private async void UpdatePlayerName(string newPlayerName)
    {
        try
        {
            _playerName = newPlayerName;
            await LobbyService.Instance.UpdatePlayerAsync(_joinedLobby.Id, AuthenticationService.Instance.PlayerId, new UpdatePlayerOptions
            {
                Data = new Dictionary<string, PlayerDataObject>
                {
                    { "PlayerName", new PlayerDataObject(PlayerDataObject.VisibilityOptions.Member, _playerName) }
                }
            }); 
        }
        catch (LobbyServiceException e)
        {
            Debug.Log(e);
        }

    }



    private async void KickPlayer()
    {
        //If want to kicksomeone, Make Button on each field of Player, assign this Function. Make It Public.
        //Make sure this function recieves an integer, that stands for the index of that player, so the right one gets kicked!
        try
        {
            await LobbyService.Instance.RemovePlayerAsync(_joinedLobby.Id, _joinedLobby.Players[1].Id);
            //Would need to handle the Updated window for the Player that gets kicked. So Joinedlobbywindow would need to get disabled.
        }
        catch (LobbyServiceException e)
        {
            Console.WriteLine(e);
            throw;
        }
    }
    //When Host leaves Lobby, the new Host will automatically be random assigned.
    //If want second Player to be host then, just call this function
    private async void MigrateLobbyHost()
    {
        try
        {
            _hostLobby = await Lobbies.Instance.UpdateLobbyAsync(_hostLobby.Id, new UpdateLobbyOptions
            {
                HostId = _joinedLobby.Players[1].Id
            });
            _joinedLobby = _hostLobby;
        }
        catch (LobbyServiceException e)
        {
            Debug.Log(e);
        }
    }
    
    //This one would be For deleting the lobby, but it would be automatically deleted.
    private void DeleteLobby()
    {
        try
        {
            LobbyService.Instance.DeleteLobbyAsync(_joinedLobby.Id);
        }
        catch (LobbyServiceException e)
        {
            Debug.Log(e);
        }
    }
}
