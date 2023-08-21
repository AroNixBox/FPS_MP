using System;
using System.Collections.Generic;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Lobbies;
using UnityEngine;
using Unity.Services.Lobbies.Models;


public class LobbyManager : MonoBehaviour
{
    private Lobby _hostLobby;
    private Lobby _joinedLobby;
    private float _heartbeatTimer;
    private float _lobbyUpdateTimer;
    private float _refreshLobbyListTimer;
    //The Name of the Player, should be festgelegt by player at its own later on
    private string _playerName;
    
    //Events

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


    /*private async void Start()
    {
        //Wait for Connection
        await UnityServices.InitializeAsync();

        AuthenticationService.Instance.SignedIn += () =>
        {
            //When Signed in, give connection, show playerID
            Debug.Log("Signed in " + AuthenticationService.Instance.PlayerId);
        };
        //Instead of PlayerLogin => Will connect anonymously
        await AuthenticationService.Instance.SignInAnonymouslyAsync();
        
        //Giving the Player a Test-Name
        _playerName = "Tester" + UnityEngine.Random.Range(10, 999);
        Debug.Log(_playerName);
    }*/

    private void Update()
    {
        //Prevent Lobby from being closed after 30 Seconds, Sends Heartbeat to Server
        HandleLobbyHeartbeat();
        HandleLobbyPollForUpdates();
        HandleRefreshLobbyList();
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
        
        //Handle RemoveInput/Auth Window
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
                    {"Map", new DataObject(DataObject.VisibilityOptions.Public, "DustyMountains")}
                }
            };
            
            
            //lobbyName + maxPlayers should be set + the Info if the created lobby should be private or not
            Lobby lobby = await LobbyService.Instance.CreateLobbyAsync(lobbyName, maxPlayers, createLobbyOptions);
        
            //Important for Heartbeat
            _hostLobby = lobby;
            _joinedLobby = _hostLobby;
            
            PrintPlayers(_hostLobby);
            Debug.Log("Created Lobby: " + lobby.Name + " " + lobby.MaxPlayers + " " + lobby.Id + " " + lobby.LobbyCode);
        }
        catch (LobbyServiceException e)
        {
            Debug.Log(e);
        }
        
    }

    /*public async void ListLobbies()
    {
        //These Filters created, but would need to be activated via button
        try
        { 
            QueryLobbiesOptions queryLobbiesOptions = new QueryLobbiesOptions
            {
                //maximum Lobbies shown in List
                Count = 25,
                Filters = new List<QueryFilter>
                {
                    //Lobbys available slots must be GT(Greater than) "0".
                    new QueryFilter(QueryFilter.FieldOptions.AvailableSlots, "0", QueryFilter.OpOptions.GT),
                    //Overwriting the free S1 Filter, Calling it CaputeTheFlag and giving it equals. If Player wants to Find CaptureTheFlag, He searched for s1 and gets all s1 Lobbies Shown.
                    //This Filter will Try to find each GameMode "CaptureTheFlag", that is public
                    //>>new Querilter(QueryFilter.FieldOptions.S1, "CaptureTheFlag", QueryFilter.OpOptions.EQ)<<
                },
                Order = new List<QueryOrder>
                {
                    //Sort in Ascending/ Descending Order, Only search for created Lobbies
                    new QueryOrder(false, QueryOrder.FieldOptions.Created)
                }
            };
            // This Seeks all lobbies lobby. In the () are the rules that that are applied when searching. Could put GameMode FIlter in Here, Make it Adjustable with Buttons
            QueryResponse queryResponse = await Lobbies.Instance.QueryLobbiesAsync(queryLobbiesOptions);
        
            Debug.Log("Lobbies found: " + queryResponse.Results.Count);
            foreach (Lobby lobby in queryResponse.Results)
            {
                //Show the Lobbies Name, its assigned MaxPlayers and the GameMode
                Debug.Log(lobby.Name + " " + lobby.MaxPlayers + " " + lobby.Data["GameMode"].Value);
            }
            
        }
        catch (LobbyServiceException e)
        {
            Debug.Log(e);
        }
    }*/

    //Autorefresh every 5 Seconds
    private void HandleRefreshLobbyList()
    {
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

    public async void RefreshLobbyList()
    {
        try
        {
            QueryLobbiesOptions options = new QueryLobbiesOptions();
            options.Count = 25;

            //Filter for open lobbies only
            options.Filters = new List<QueryFilter>
            {
                new QueryFilter(field: QueryFilter.FieldOptions.AvailableSlots,
                    op: QueryFilter.OpOptions.GT, value: "0")
            };
            options.Order = new List<QueryOrder>
            {
                new QueryOrder(
                    asc: false,
                    field: QueryOrder.FieldOptions.Created)
            };
            QueryResponse lobbyListQueryResponse = await Lobbies.Instance.QueryLobbiesAsync();
            
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

    public async void LeaveLobby()
    {
        try
        {
            await LobbyService.Instance.RemovePlayerAsync(_joinedLobby.Id, AuthenticationService.Instance.PlayerId);
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
