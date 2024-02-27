using Godot;
using Godot.Collections;
using Microsoft.VisualBasic;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text.Json;

public partial class SMuL : Node, SMuLUser
{
	public static SMuL instance;

	public static Lobby lobby { get; private set; }
	public static System.Collections.Generic.Dictionary<string, Lobby> lobbies { get; private set; } = new System.Collections.Generic.Dictionary<string, Lobby>();
	public static int playerId { get; private set; }

	[Export] private string gameApiKey;
	[Export] private bool developmentMode;

	[Export] private string leaderboardKey;

	[Export] private float lobbyLifetimeDays = 1.0f;
	
	private static string sessionToken = "";

	private static HttpRequest authHttp;
	private static HttpRequest leaderboardHttp;
	private static HttpRequest submitScoreHttp;

	private static Queue<SMuLRequest> requestQueue = new Queue<SMuLRequest>();
	private static SMuLRequest currRequest = null;

	[Export] private float secondsBetweenRetrys = 5.0f;
	private static float requestRetryTimer = 0;
	private static bool retryRequest = false;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		//Don't allow multiple instances.
		if (instance != null)
		{
			this.QueueFree();
			return;
		}
		instance = this;
		//Start by authenticating and getting active lobbies.
		requestQueue.Enqueue(new SMuLRequestGetLobbies(SMuLRequestType.GET_LOBBIES, instance));
		currRequest = new SMuLRequest(SMuLRequestType.AUTHENTICATE);
		AuthenticationRequest();
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		//Did not get a valid response so try again.
		//Currently only works for authentication requests.
		if (currRequest != null && retryRequest)
		{
			if (requestRetryTimer > instance.secondsBetweenRetrys)
			{
				switch (currRequest.requestType)
				{
					case SMuLRequestType.AUTHENTICATE:
						AuthenticationRequest();
						break;
					case SMuLRequestType.GET_LOBBIES:
					case SMuLRequestType.LOBBY_STATUS:
						GetLeaderboard();
						break;
					case SMuLRequestType.CREATE_LOBBY:
					case SMuLRequestType.JOIN_LOBBY:
					case SMuLRequestType.UPDATE_STATUS:
						SetLeaderboard(
							((SMuLRequestUpdateLeaderboard)currRequest).lobbyString,
							((SMuLRequestUpdateLeaderboard)currRequest).playerScore);
						break;
				}
				retryRequest = false;
			}
			requestRetryTimer += (float)delta;
		}
		//Start making a request if there isn't an active one.
		if (currRequest == null && requestQueue.Count > 0)
		{
			currRequest = requestQueue.Dequeue();
			switch (currRequest.requestType)
			{
				case SMuLRequestType.AUTHENTICATE:
					//Authenticate the player.
					AuthenticationRequest();
					break;
				case SMuLRequestType.GET_LOBBIES:
				case SMuLRequestType.LOBBY_STATUS:
					//Get the leaderboard and update global variables.
					GetLeaderboard();
					break;
				case SMuLRequestType.CREATE_LOBBY:
					//Decode the lobbyString.
					object lobbyObj;
					LobbyString.DecodeString(((SMuLRequestUpdateLeaderboard)currRequest).lobbyString).TryGetValue("lobbyName", out lobbyObj);
					string lobbyName = (string)lobbyObj;
					LobbyString.DecodeString(((SMuLRequestUpdateLeaderboard)currRequest).lobbyString).TryGetValue("startTime", out lobbyObj);
					DateTime lobbyStartTime = (DateTime)lobbyObj;
					LobbyString.DecodeString(((SMuLRequestUpdateLeaderboard)currRequest).lobbyString).TryGetValue("playerCount", out lobbyObj);
					int playerCount = (int)lobbyObj;
					LobbyString.DecodeString(((SMuLRequestUpdateLeaderboard)currRequest).lobbyString).TryGetValue("metadata", out lobbyObj);
                    System.Collections.Generic.Dictionary<string, string> metadata = (System.Collections.Generic.Dictionary<string, string>)lobbyObj;
					bool lobbyInUse = false;

					//If the lobby already exists, do not create a new lobby.
					foreach (Lobby lobby in lobbies.Values)
					{
						if (lobby.name.Equals(lobbyName))
						{
							if (lobby.players[0].gameEnded == false)
							{
								lobbyInUse = true;
								break;
							}
						}
					}
					if (lobbyInUse)
					{
						currRequest = null;
						break;
					}

					//Create a new lobby.
					lobby = new Lobby(lobbyName, playerCount, metadata);
					playerId = 0;
					lobby.players[0] = new PlayerState(((SMuLRequestUpdateLeaderboard)currRequest).lobbyString, lobbyStartTime, ((SMuLRequestUpdateLeaderboard)currRequest).playerScore);

					//Add the lobby to the leaderboard.
					SetLeaderboard(
						((SMuLRequestUpdateLeaderboard)currRequest).lobbyString,
						((SMuLRequestUpdateLeaderboard)currRequest).playerScore);
					break;
				case SMuLRequestType.JOIN_LOBBY:
					string lobbyToJoinLobbyString = ((SMuLRequestUpdateLeaderboard)currRequest).lobbyString;
					
					//Decode the lobbyString.
					object lobbyToJoinNameObj;
					LobbyString.DecodeString(lobbyToJoinLobbyString).TryGetValue("lobbyName", out lobbyToJoinNameObj);
					string lobbyToJoinName = (string)lobbyToJoinNameObj;
					LobbyString.DecodeString(lobbyToJoinLobbyString).TryGetValue("startTime", out lobbyToJoinNameObj);
					DateTime playerJoinTime = (DateTime)lobbyToJoinNameObj;
					LobbyString.DecodeString(lobbyToJoinLobbyString).TryGetValue("metadata", out lobbyToJoinNameObj);
					System.Collections.Generic.Dictionary<string, string> playerJoiningMetadata = (System.Collections.Generic.Dictionary<string, string>)lobbyToJoinNameObj;

					//Make sure the lobby exists and is available.
					Lobby lobbyToJoin;
					bool lobbyExist = lobbies.TryGetValue(lobbyToJoinName, out lobbyToJoin);
					if (!lobbyExist || lobbyToJoin.IsLobbyFull() || lobbyToJoin.players[0] == null || lobbyToJoin.players[0].gameStarted)
					{
						currRequest = null;
						break;
					}

					//Lobby exists, so make a new lobbyString with the right playerId and playerAmount.
					string actualLobbyString = LobbyString.EncodeString(lobbyToJoinName, playerJoinTime, lobbyToJoin.playerAmt, lobbyToJoin.PlayersPresent(), playerJoiningMetadata);

					//Set global variables.
					lobby = lobbyToJoin;
					playerId = lobbyToJoin.PlayersPresent();
					lobby.players[playerId] = new PlayerState(lobbyToJoinLobbyString, playerJoinTime, ((SMuLRequestUpdateLeaderboard)currRequest).playerScore);

					//Add player to the leaderboard.
					((SMuLRequestUpdateLeaderboard)currRequest).UpdateLobbyString(actualLobbyString);

					SetLeaderboard(
						((SMuLRequestUpdateLeaderboard)currRequest).lobbyString,
						((SMuLRequestUpdateLeaderboard)currRequest).playerScore);

					break;
				case SMuLRequestType.UPDATE_STATUS:
					//Update your players status.
					SetLeaderboard(
						((SMuLRequestUpdateLeaderboard)currRequest).lobbyString,
						((SMuLRequestUpdateLeaderboard)currRequest).playerScore);
					break;
			}
		}
	}

	/// <summary>
	/// Send an authentication request. If you have authenticated in the past, reuse details.
	/// </summary>
	/// <exception cref="NullReferenceException"></exception>
	private static void AuthenticationRequest()
	{
		//If you do not have a gameApiKey or leaderboardKey, do not authenticate.
		if (string.IsNullOrEmpty(instance.gameApiKey) || string.IsNullOrEmpty(instance.leaderboardKey))
		{
			throw new NullReferenceException("gameApiKey and leaderboardKey must have a value");
		}

		//Check for previous authentication details. (Prevents duplicate monthly users)
		bool playerSessionExists = false;
		string playerIdentifier = null;
		FileAccess file = FileAccess.Open("user://LootLocker.data", FileAccess.ModeFlags.Read);
		if (file != null)
		{
			playerIdentifier = file.GetAsText();
			GD.Print("player ID = " +  playerIdentifier);
			file.Close();
		}

		if (playerIdentifier != null && playerIdentifier.Length > 1)
		{
			GD.Print("player session exists , id = " + playerIdentifier);
			playerSessionExists = true;
		}

		//Create request variables.
		string data = JsonSerializer.Serialize(new {
			game_key = instance.gameApiKey, 
			game_version = "0.0.0.1", 
			development_mode = instance.developmentMode});

		if (playerSessionExists)
		{
			data = JsonSerializer.Serialize(new {
				game_key = instance.gameApiKey,
				player_identifier = playerIdentifier,
				game_version = "0.0.0.1",
				development_mode = instance.developmentMode});
		}
		string[] headers = { "Content-Type: application/json" };
		authHttp = new HttpRequest();
		instance.AddChild(authHttp);
		authHttp.RequestCompleted += AuthenticationRequestCompleted;
		
		//Make the authentication request.
		authHttp.Request("https://api.lootlocker.io/game/v2/session/guest", headers, HttpClient.Method.Post, data);
		GD.Print(data);
	}

	/// <summary>
	/// When an authentication request is completed, store session token and player details.
	/// </summary>
	/// <param name="result"></param>
	/// <param name="responseCode"></param>
	/// <param name="headers"></param>
	/// <param name="body"></param>
	private static void AuthenticationRequestCompleted(long result, long responseCode, string[] headers, byte[] body)
	{
		//Convert data to readable Json.
		Json json = new Json();
		json.Parse(body.GetStringFromUtf8());

		//Store player information in a file.
		FileAccess file = FileAccess.Open("user://LootLocker.data", FileAccess.ModeFlags.Write);
		Variant playerIdentifierVariant;
		json.Data.AsGodotDictionary().TryGetValue("player_identifier", out playerIdentifierVariant);
		file.StoreString((string)playerIdentifierVariant);
		file.Close();

		//Store session token in a global variable.
		Variant sessionTokenVariant;
		json.Data.AsGodotDictionary().TryGetValue("session_token", out sessionTokenVariant);
		sessionToken = (String)sessionTokenVariant;

		GD.Print(json.Data);

		//Free the authentication node used to make the request.
		authHttp.QueueFree();
		
		if (!string.IsNullOrEmpty(sessionToken))
		{
			//If authentication succeeded, clear currRequest for next request.
			currRequest = null;
		} 
		else
		{
			//If authentication failed, begin retry process.
			retryRequest = true;
			requestRetryTimer = 0.0f;
		}
	}

	/// <summary>
	/// Get all lobbies present and send them to a user through user.ReceiveLobbies(Dictionary(string, Lobby)).
	/// Additionally update global variables.
	/// </summary>
	/// <param name="user"></param>
	public static void GetLobbies(SMuLUser user)
	{
		//Generate request.
		SMuLRequestGetLobbies request = new SMuLRequestGetLobbies(SMuLRequestType.GET_LOBBIES, user);
		requestQueue.Enqueue(request);
	}

	/// <summary>
	/// Get the lobby the player is currently in and send it to a user through user.ReceiveLobby(Lobby).
	/// Additionally update global variables.
	/// </summary>
	/// <param name="user"></param>
	public static void GetLobby(SMuLUser user)
	{
		if (lobby == null)
		{
			return;
		}
		SMuLRequestGetLobby request = new SMuLRequestGetLobby(SMuLRequestType.LOBBY_STATUS, user, lobby.players[playerId].lobbyString);
		requestQueue.Enqueue(request);
	}

	/// <summary>
	/// Send a get leaderboard request.
	/// </summary>
	/// <exception cref="NullReferenceException"></exception>
	private static void GetLeaderboard()
	{
		//If there is no session token, do not run.
		if (string.IsNullOrEmpty(sessionToken))
		{
			throw new NullReferenceException("No session token found. Could not get leaderboard.");
		}

		//Create request variables.
		string url = "https://api.lootlocker.io/game/leaderboards/" + instance.leaderboardKey + "/list?count=2000";
		string[] headers = { "Content-Type: application/json", "x-session-token: " + sessionToken };

		leaderboardHttp = new HttpRequest();
		instance.AddChild(leaderboardHttp);
		leaderboardHttp.RequestCompleted += GetLeaderboardRequestCompleted;

		//Send the get leaderboard request.
		leaderboardHttp.Request(url, headers, HttpClient.Method.Get, "");
	}

	/// <summary>
	/// Update global variables with data received from the request. Send Lobby/Lobbies to a user if it exists.
	/// </summary>
	/// <param name="result"></param>
	/// <param name="responseCode"></param>
	/// <param name="headers"></param>
	/// <param name="body"></param>
	/// <exception cref="NullReferenceException"></exception>
	private static void GetLeaderboardRequestCompleted(long result, long responseCode, string[] headers, byte[] body)
	{
		//Make sure request exists.
		if (currRequest == null)
		{
			throw new NullReferenceException("Request not found.");
		}

		//Convert data to Json.
		Json json = new Json();
		json.Parse(body.GetStringFromUtf8());

		GD.Print(json.Data);

		//Switch lobbies with new lobby information.
		//individual lobbies within lobbies will not have players[].playerHasBeenUpdated set.
		lobbies.Clear();

		//Convert data to an array of player information.
		Dictionary lobbiesDictionaryGodot = json.Data.AsGodotDictionary();
		Variant lobbiesArrayGodotVariant;
		lobbiesDictionaryGodot.TryGetValue("items", out lobbiesArrayGodotVariant);
		Godot.Collections.Array lobbiesArrayGodot = (Godot.Collections.Array)lobbiesArrayGodotVariant;
		foreach (Dictionary items in lobbiesArrayGodot)
		{
			//Decode the player's name into player information.
			Variant lobbiesDictionaryGodotVariant;
			items.TryGetValue("member_id", out lobbiesDictionaryGodotVariant);
			string lobbyString = (string)lobbiesDictionaryGodotVariant;
			object lobbyObj;
			System.Collections.Generic.Dictionary<string, object> lobbyDictionary = LobbyString.DecodeString(lobbyString);
			lobbyDictionary.TryGetValue("lobbyName", out lobbyObj);
			string lobbyName = (string)lobbyObj;
			lobbyDictionary.TryGetValue("startTime", out lobbyObj);
			DateTime playerJoinTime = (DateTime)lobbyObj;
			lobbyDictionary.TryGetValue("playerCount", out lobbyObj);
			int lobbyPlayerCount = (int)lobbyObj;
			lobbyDictionary.TryGetValue("playerId", out lobbyObj);
			int playerId = (int)lobbyObj;
			lobbyDictionary.TryGetValue("metadata", out lobbyObj);
            System.Collections.Generic.Dictionary<string, string> metadata = (System.Collections.Generic.Dictionary<string, string>)lobbyObj;

			//Store the players score to be used as a PlayerState.
			items.TryGetValue("score", out lobbiesDictionaryGodotVariant);
			int playerState = (int)lobbiesDictionaryGodotVariant;

			PlayerState testPlayerState = new PlayerState(lobbyString, playerJoinTime, playerState);
			if (testPlayerState.gameEnded)
			{
				//If the player's game has ended ignore its existance.
				continue;
			} 
			else if (testPlayerState.playerJoinTime.Subtract(DateTimeOffset.Now.DateTime).Duration().TotalDays > instance.lobbyLifetimeDays)
			{
				//If the player's game has gone on for too long, end it.
				testPlayerState.DisconnectPlayer();
				requestQueue.Enqueue(new SMuLRequestUpdateLeaderboard(SMuLRequestType.UPDATE_STATUS, lobbyString, testPlayerState.GetScore()));
			}

			//Check if the player's lobby already exists.
			Lobby lobby = null;
			foreach (string lobbyKey in lobbies.Keys)
			{
				if (lobbyKey.Equals(lobbyName))
				{
					lobbies.TryGetValue(lobbyKey, out lobby);
					break;
				}
			}

			//If the player's lobby doesn't exist create it.
			if (lobby == null)
			{
				lobby = new Lobby(lobbyName, lobbyPlayerCount, metadata);
				lobbies.Add(lobbyName, lobby);
			}

			//Add player to the lobby.
			if (lobby.players[playerId] == null)
			{
				//Sinse lobbies gets cleared and recreated, the player should not exist prior and should be created here.
				lobby.players[playerId] = new PlayerState(lobbyString, playerJoinTime, playerState);
			}
			else
			{
				//Sinse lobbies gets cleared and recreated, the player should not exist prior and should not be updated here.
				lobby.players[playerId].UpdateScore(playerState);
			}

			GD.Print(lobbyString);
		}

		//If the player is in a lobby, update their lobby's state.
		if (lobby != null)
		{
			Lobby updatedLobby;
			lobbies.TryGetValue(lobby.name, out updatedLobby);

			lobby.UpdatePlayers(updatedLobby);
		}

		//If there is a user, send them the Lobby/Lobbies.
		if (((SMuLRequestGetLobbies)currRequest).user != null)
		{
			switch (currRequest.requestType)
				{
					case SMuLRequestType.GET_LOBBIES:
						((SMuLRequestGetLobbies)currRequest).user.ReceiveLobbies(lobbies);
						break;
					case SMuLRequestType.LOBBY_STATUS:
						((SMuLRequestGetLobby)currRequest).user.ReceiveLobby(lobby);
						break;
					default:
						break;
				}
		}
		
		//Free the leaderboard node.
		leaderboardHttp.QueueFree();

		//Clear currRequest for next request.
		currRequest = null;
	}

	/// <summary>
	/// Creates a lobby as long as a lobby does not already exist with the same name.
	/// </summary>
	/// <param name="lobbyName"></param>
	/// <param name="playerAmt"></param>
	public static void CreateLobby(string lobbyName, int playerAmt)
	{
		//Start by updating lobbies.
		requestQueue.Enqueue(new SMuLRequestGetLobbies(SMuLRequestType.GET_LOBBIES, instance));
		
		//Create new lobbyString.
		string lobbyString = LobbyString.EncodeString(
			lobbyName,
			DateTimeOffset.Now.DateTime,
			playerAmt,
			0);
		int playerScore = 0;

		//Try to create a new lobby.
		requestQueue.Enqueue(new SMuLRequestUpdateLeaderboard(SMuLRequestType.CREATE_LOBBY, lobbyString, playerScore));
	}

	/// <summary>
	/// Joins a lobby if it exists and if there is room for another player.
	/// </summary>
	/// <param name="lobbyName"></param>
	public static void JoinLobby(string lobbyName)
	{
		//Make sure the lobby exists.
		Lobby lobby;
		bool lobbyExist = lobbies.TryGetValue(lobbyName, out lobby);
		if (!lobbyExist || lobby.IsLobbyFull() || lobby.players[0] == null || lobby.players[0].gameStarted)
		{
			return;
		}

		//Update lobbies.
		requestQueue.Enqueue(new SMuLRequestGetLobbies(SMuLRequestType.GET_LOBBIES, instance));

		//Create new lobby string for the new player.
		//This will not be directly used sinse the player's Id will be taken from the updated lobby's player amount.
		string lobbyString = LobbyString.EncodeString(
			lobbyName,
			DateTimeOffset.Now.DateTime,
			lobby.playerAmt,
			-1);
		int playerScore = 0;

		//Try to join the lobby.
		requestQueue.Enqueue(new SMuLRequestUpdateLeaderboard(SMuLRequestType.JOIN_LOBBY, lobbyString, playerScore));
	}

	/// <summary>
	/// Update the player's online state.
	/// </summary>
	public static void UpdateState()
	{
		//Check to make sure the lobby and player exists.
		if (lobby == null || lobby.players[playerId] == null)
		{
			return;
		}

		//Create a lobbyString.
		string lobbyString = LobbyString.EncodeString(
			lobby.name,
			lobby.players[playerId].playerJoinTime,
			lobby.playerAmt,
			playerId);

		int playerScore = lobby.players[playerId].GetScore();

		//Try to update the player's score.
		requestQueue.Enqueue(new SMuLRequestUpdateLeaderboard(SMuLRequestType.UPDATE_STATUS, lobbyString, playerScore));
	}

	/// <summary>
	/// Update or create an entry of the leaderboard.
	/// </summary>
	/// <param name="lobbyString"></param>
	/// <param name="score"></param>
	/// <exception cref="NullReferenceException"></exception>
	private static void SetLeaderboard(string lobbyString, int score)
	{
		//If there is no session token.
		if (string.IsNullOrEmpty(sessionToken))
		{
			throw new NullReferenceException("No session token found. Could not get leaderboard.");
		}

		//Create request variables
		string data = JsonSerializer.Serialize(new {
			score = score,
			member_id = lobbyString});
		string[] headers = { "Content-Type: application/json", "x-session-token:" + sessionToken };
		submitScoreHttp = new HttpRequest();
		instance.AddChild(submitScoreHttp);
		submitScoreHttp.RequestCompleted += SetLeaderboardRequestCompleted;

		//Send the set leaderboard request
		submitScoreHttp.Request("https://api.lootlocker.io/game/leaderboards/" + instance.leaderboardKey + "/submit", headers, HttpClient.Method.Post, data);
		GD.Print(data);
	}

	/// <summary>
	/// Handle clean up after a set score request is completed.
	/// </summary>
	/// <param name="result"></param>
	/// <param name="responseCode"></param>
	/// <param name="headers"></param>
	/// <param name="body"></param>
	private static void SetLeaderboardRequestCompleted(long result, long responseCode, string[] headers, byte[] body)
	{
		Json json = new Json();
		json.Parse(body.GetStringFromUtf8());

		GD.Print(json.Data);

		//Free the submit score node.
		submitScoreHttp.QueueFree();

		//Clear currRequest for the next request.
		currRequest = null;
	}

	public void ReceiveLobbies(System.Collections.Generic.Dictionary<string, Lobby> lobbies)
	{
		//When server does internal checks use this.
	}
}
