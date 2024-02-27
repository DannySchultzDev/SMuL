using Godot;
using System;

public partial class WaitForPlayersUI : Control, SMuLUser
{
	private bool shouldCheck = false;

	private float time = 0;
	private static float updateWaitTime = 2;

	[Export] private PackedScene testGame;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		//Wait until this is visible to start checking.
		if (!shouldCheck)
		{
			return;
		}
		time += (float)delta;
		if (time > updateWaitTime)
		{
			time -= updateWaitTime;

			//Check to see if the lobby is full.
			//If the lobby is full start the game.
			SMuL.GetLobby(this);
		}
	}

	public void ReceiveLobby(Lobby lobby)
	{
		//Set label text.
		((Label)GetChild(0)).Text = "Waiting for players.\n" +
			lobby.PlayersPresent() + " out of " + lobby.playerAmt + " players found.\n" +
			"You are player " + (SMuL.playerId + 1) + ".";
		//If lobby is full set startGame to true.
		if (lobby.PlayersPresent() == lobby.playerAmt && !lobby.players[SMuL.playerId].gameStarted)
		{
			lobby.players[SMuL.playerId].StartGame();
			SMuL.UpdateState();
		}
		else if (lobby.PlayersPresent() == lobby.playerAmt) 
		{
			//If the lobby is full and everyone is present (no one left the lobby), start the game.
			for (int i = 0; i < lobby.playerAmt; ++i)
			{
				if (!lobby.players[i].gameStarted)
				{
					return;
				}
			}
			for (int i = 0; i < lobby.playerAmt; ++i)
			{
				//Resets the player's state and playerHasBeenUpdated so the first turn can be detected.
				lobby.players[i].FinishTurn(0);
				lobby.players[i].FinishReadTurn();
			}
			TestGame game = (TestGame)testGame.Instantiate();
			GetParent().AddSibling(game);
			GetParent().QueueFree();
		}
	}

	/// <summary>
	/// Start checking for when the lobby is full.
	/// </summary>
	public void StartChecking()
	{
		Visible = true;
		shouldCheck = true;
	}
}
