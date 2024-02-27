using Godot;
using Godot.Collections;
using System;
using System.Collections;
using System.Collections.Generic;

public partial class ServersTree : Tree, SMuLUser
{
	private float time = 0;
	private static float updateWaitTime = 3;

	[Export] LineEdit lineEdit;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		HideRoot = true;
		SMuL.GetLobbies(this);
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		//Check the leaderboard for lobbies every few seconds.
		time += (float)delta;
		if (time > updateWaitTime) 
		{
			time -= updateWaitTime;
			SMuL.GetLobbies(this);
		}
	}

	public void ReceiveLobbies(System.Collections.Generic.Dictionary<string, Lobby> lobbies)
	{
		//Don't do anything if this no longer exists.
		if (!IsInstanceValid(this)) {
			return;
		}
		Clear();
		GD.Print(lobbies);
		
		//Invisible header.
		TreeItem header = CreateItem();
		//Visiblee header.
		TreeItem headerText = CreateItem();
		headerText.SetText(0, "Lobby Name");
		headerText.SetText(1, "Current Players");
		headerText.SetText(2, "Max Players");
		foreach (string lobbyName in lobbies.Keys)
		{
			//Add each lobby to the tree.
			Lobby lobby;
			lobbies.TryGetValue(lobbyName, out lobby);
			if (!lobby.players[0].gameEnded)
			{
				TreeItem item = CreateItem();
				item.SetText(0, lobbyName);
				item.SetText(1, lobby.PlayersPresent().ToString());
				item.SetText(2, lobby.playerAmt.ToString());
				if (lobby.players[0].gameStarted || lobby.PlayersPresent() >= lobby.playerAmt)
				{
					//If the lobby is full/if the game has started dim the text color
					for (int i = 0; i < 3; ++i)
					{
						item.SetCustomColor(i, Color.Color8(100, 100, 100));
					}
				}
			}
		}
	}

	/// <summary>
	/// Set the lineEdit to the name of the lobby selected.
	/// </summary>
	public void cell_selected()
	{
		if (GetSelected().GetIndex() == 0)
		{
			return;
		}
		lineEdit.Text = GetSelected().GetText(0);
	}
}
