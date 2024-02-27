using Godot;
using System;
using System.Collections.Generic;

public class Lobby
{
	public readonly string name;
	public readonly int playerAmt;
	public readonly PlayerState[] players;
	public IReadOnlyDictionary<string, string> metadata;

	public Lobby(string name, int playerAmt, Dictionary<string, string> metadata = null)
	{
		this.name = name;
		this.playerAmt = playerAmt;
		this.metadata = metadata;

		players = new PlayerState[playerAmt];
	}

	/// <summary>
	/// Detect if all players are present in the lobby.
	/// </summary>
	/// <returns></returns>
	public bool IsLobbyFull()
	{
		for (int i = 0; i < playerAmt; ++i)
		{
			if (players[i] == null || players[i].gameEnded)
			{
				return false;
			}
		}
		return true;
	}

	/// <summary>
	/// Find how many players are present in the lobby.
	/// </summary>
	/// <returns>The number of players in the lobby</returns>
	public int PlayersPresent()
	{
		int playersPresent = 0;
		for (int i = 0; i < playerAmt; ++i)
		{
			if (players[i] != null && !players[i].gameEnded)
			{
				playersPresent++;
			}
		}
		return playersPresent;
	}

	/// <summary>
	/// Updates the status of each player in the lobby to the new lobby<br/>
	/// This will set playerHasBeenUpdated if the player's status has changed.
	/// </summary>
	/// <param name="lobby"></param>
	public void UpdatePlayers(Lobby lobby)
	{
		//If the lobbies have different numbers of players, do not run.
		if (playerAmt != lobby.playerAmt)
		{
			return;
		}

		for (int i = 0; i < playerAmt; ++i)
		{
			//Ensure the original player exists before calling UpdateScore.
			if (players[i] == null || lobby.players[i] == null)
			{
				//If the old player does not exist, just set it directly.
				players[i] = lobby.players[i];
			}
			else
			{
				players[i].UpdateScore(lobby.players[i].GetScore());
			}
		}
	}
}
