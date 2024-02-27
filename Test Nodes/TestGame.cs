using Godot;
using System;

public partial class TestGame : Node2D, SMuLUser
{
	[Export] private TestPlayer[] players;

	[Export] private Label playerIdLabel;
	[Export] private Label currentPlayerLabel;
	[Export] private Label lastRollLabel;

	private int playerTurn = 0;
	private float time = 0;
	private static float updateWaitTime = 3;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		//Make sure player is in a lobby.
		if (SMuL.lobby == null)
		{
			QueueFree();
		}
		//Sets players based on the lobby's playerAmt.
		for (int i = 3; i >= SMuL.lobby.playerAmt; --i)
		{
			players[i].Visible = false;
		}
		//Set text.
		playerIdLabel.Text = "You are player " + (SMuL.playerId + 1) + ".";
		SetCurrentPlayerLabel();
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		//Check lobby for updates every few seconds.
		time += (float)delta;
		if (time > updateWaitTime)
		{
			time -= updateWaitTime;
			SMuL.GetLobby(this);
		}
	}

	public override void _Input(InputEvent @event)
	{
		//When Space is pressed on the player's turn, roll a simulated D6.
		if (@event is InputEventKey eventKey &&
			eventKey.Pressed && eventKey.Keycode == Key.Space &&
			playerTurn == SMuL.playerId)
		{
			Random rand = new Random();
			int movement = (int)rand.NextInt64(1, 7);
			players[playerTurn].Move(movement);
			SetLastRollLabel(movement);
			//Check if the player won.
			if (players[playerTurn].pos >= 25)
			{
				Label winLabel = new Label();
				winLabel.Text = "You won!";
				GetParent().AddChild(winLabel);
				SMuL.lobby.players[SMuL.playerId].FinishTurn(movement);
				SMuL.lobby.players[SMuL.playerId].FinishReadTurn();
				SMuL.UpdateState();
				QueueFree();
				return;
			}
			//If the player did not win update turn.
			playerTurn = (playerTurn + 1) % SMuL.lobby.playerAmt;
			SetCurrentPlayerLabel();
			SMuL.lobby.players[SMuL.playerId].FinishTurn(movement);
			SMuL.lobby.players[SMuL.playerId].FinishReadTurn();
			SMuL.UpdateState();
		}
	}

	public void ReceiveLobby(Lobby lobby)
	{
		if (lobby.players[playerTurn].playerHasBeenUpdated)
		{
			//If the current player has rolled, simmulate their roll.
			players[playerTurn].Move(lobby.players[playerTurn].playerState);
			lobby.players[playerTurn].FinishReadTurn();
			//Check if the player has won.
			if (players[playerTurn].pos >= 25)
			{
				Label winLabel = new Label();
				winLabel.Text = "Player " + (playerTurn + 1) + " won!";
				GetParent().AddChild(winLabel);
				QueueFree();
				return;
			}
			//If the player did not win, move to the next turn.
			playerTurn = (playerTurn + 1) % lobby.playerAmt;
			SetCurrentPlayerLabel();
		}
	}

	/// <summary>
	/// Sets the currentPlayerLabel.
	/// </summary>
	private void SetCurrentPlayerLabel()
	{
		currentPlayerLabel.Text = "It is currently " + (playerTurn == SMuL.playerId ? "your" : "player " + (playerTurn + 1) + "'s") + " turn.";
	}

	/// <summary>
	/// Sets the lastRollLabel.
	/// </summary>
	/// <param name="lastRoll"></param>
	private void SetLastRollLabel(int lastRoll)
	{
		lastRollLabel.Text = "Your last roll was a " + lastRoll + ".";
	}
}
