using Godot;
using System;

public partial class MainMenuUI : Control
{
	private float initialCheckTime = 0.2f;
	private float secondaryCheckTime = 1.0f;
	private int numChecks = 0;
	private float currTime = 0.0f;

	[Export] private WaitForPlayersUI waitForPlayersUI;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		//Only check after pressing a button.
		if (numChecks < 1)
		{
			return;
		}
		currTime += (float)delta;
		//First check happens quickly, other checks happen slower.
		float timeBetweenChecks = numChecks > 9 ? initialCheckTime : secondaryCheckTime;
		if (currTime >= timeBetweenChecks)
		{
			--numChecks;
			currTime = 0.0f;
			if (SMuL.lobby != null)
			{
				//When lobby is present, switch main menu screen for wait for players screen.
				waitForPlayersUI.StartChecking();
				SMuL.GetLobby(waitForPlayersUI);
				QueueFree();
			}
		}
	}

	/// <summary>
	/// Starts checking for the lobby.
	/// </summary>
	public void StartChecking()
	{
		numChecks = 10;
	}
}
