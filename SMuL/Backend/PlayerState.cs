using Godot;
using System;

public class PlayerState
{
	public string lobbyString { get; private set; }
	public DateTime playerJoinTime { get; private set; }
	public int playerState { get; private set; }
	public bool gameStarted { get; private set; }
	public bool gameEnded { get; private set; }
	public bool turnEven { get; private set; }
	public bool playerHasBeenUpdated { get; private set; }
	public PlayerState(string lobbyString, DateTime playerJoinTime, int playerScore)
	{
		this.lobbyString = lobbyString;
		this.playerJoinTime = playerJoinTime;
		playerState = 0;
		gameStarted = false;
		gameEnded = false;
		turnEven = true;
		playerHasBeenUpdated = false;

		UpdateScore(playerScore);
	}

	/// <summary>
	/// Updates the values of the PlayerState to the values decoded from a score.<br/>
	/// If anything has changed, set playerHasBeenUpdated.
	/// </summary>
	/// <param name="playerScore"></param>
	public void UpdateScore (int playerScore)
	{
		//Decode the score.
		int playerState = playerScore >> 3;
		playerScore -= playerState << 3;
		bool gameStarted = playerScore >> 2 == 1;
		playerScore -= (gameStarted ? 1 : 0) << 2;
		bool gameEnded = playerScore >> 1 == 1;
		playerScore -= (gameEnded ? 1 : 0) << 1;
		bool turnEven = playerScore == 1;

		//Check if something has changed.
		if (this.playerState != playerState || 
			this.gameStarted != gameStarted ||
			this.gameEnded != gameEnded ||
			this.turnEven != turnEven)
		{
			playerHasBeenUpdated = true;
		}

		//Set variables.
		this.playerState = playerState;
		this.gameStarted = gameStarted;
		this.gameEnded = gameEnded;
		this.turnEven = turnEven;
	}

	/// <summary>
	/// Encodes the PlayerState as a score.
	/// </summary>
	/// <returns>The state encoded as a score</returns>
	public int GetScore ()
	{
		int score = playerState << 3;
		score += (gameStarted ? 1 : 0) << 2;
		score += (gameEnded ? 1 : 0) << 1;
		score += turnEven ? 1 : 0;
		return score;
	}

	/// <summary>
	/// Trys to set gameStarted to true.<br/>
	/// If gameStarted was already true, does not change playerHasBeenUpdated.<br/>
	/// Otherwise sets playerHasBeenUpdated to true.
	/// </summary>
	public void StartGame()
	{
		if (gameStarted)
		{
			return;
		}
		this.gameStarted = true;
		playerHasBeenUpdated = true;
	}

	/// <summary>
	/// Trys to set gameEnded to true.<br/>
	/// If gameEnded was already true, does not change playerHasBeenUpdated.<br/>
	/// Otherwise sets playerHasBeenUpdated to true.
	/// </summary>
	public void DisconnectPlayer()
	{
		if (gameEnded)
		{
			return;
		}
		gameEnded = true;
		playerHasBeenUpdated = true;
	}

	/// <summary>
	/// Sets the player's state to the new state.<br/>
	/// Flips the turn between even and odd.
	/// </summary>
	/// <param name="newState"></param>
	public void FinishTurn(int newState)
	{
		playerState = newState;
		turnEven = !turnEven;
		playerHasBeenUpdated = true;
	}

	/// <summary>
	/// Sets playerHasBeenUpdated to false.<br/>
	/// Call this after reading a player's updated state.
	/// </summary>
	public void FinishReadTurn()
	{
		playerHasBeenUpdated = false;
	}
}
