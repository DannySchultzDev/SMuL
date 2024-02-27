using Godot;
using System;

public partial class JoinButton : Button
{
	[Export] private LineEdit lineEdit;

	[Export] private MainMenuUI mainMenuUi;
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}

	/// <summary>
	/// Try to join a lobby with the name in lineEdit.
	/// </summary>
	public void _on_button_down()
	{
		SMuL.JoinLobby(lineEdit.Text);

		//Tell the main menu to start checking for if/when the lobby is created.
		mainMenuUi.StartChecking();
	}
}
