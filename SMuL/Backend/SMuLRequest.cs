using Godot;
using System;

public class SMuLRequest
{
	public SMuLRequestType requestType { private set; get; }
	public DateTime startTime { private set; get; }

	public SMuLRequest(SMuLRequestType requestType)
	{
		this.requestType = requestType;
		startTime = DateTimeOffset.Now.DateTime;
	}
}

public class SMuLRequestGetLobbies : SMuLRequest
{
	public SMuLUser user { private set; get; }

	public SMuLRequestGetLobbies (SMuLRequestType requestType, SMuLUser user) : base(requestType)
	{
		this.user = user;
	}
}

public class SMuLRequestGetLobby : SMuLRequestGetLobbies
{
	public string lobbyString { private set; get; }
	public SMuLRequestGetLobby(SMuLRequestType requestType, SMuLUser user, string lobbyString) : base(requestType, user)
	{
		this.lobbyString = lobbyString;
	}
}
public class SMuLRequestUpdateLeaderboard : SMuLRequest
{
	public string lobbyString { private set; get; }
	public int playerScore { private set; get; }
	public SMuLRequestUpdateLeaderboard(SMuLRequestType requestType, string lobbyString, int playerScore) : base(requestType)
	{
		this.lobbyString = lobbyString;
		this.playerScore = playerScore;
	}

	/// <summary>
	/// Update the current lobbyString to a new lobbyString.
	/// </summary>
	/// <param name="updatedString"></param>
	public void UpdateLobbyString(string updatedString)
	{
		lobbyString = updatedString;
	}
}

public enum SMuLRequestType
{
	AUTHENTICATE,
	GET_LOBBIES,
	LOBBY_STATUS,
	CREATE_LOBBY,
	JOIN_LOBBY,
	UPDATE_STATUS
}
