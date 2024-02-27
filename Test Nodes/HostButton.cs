using Godot;
using System;

public partial class HostButton : Button
{
	[Export] private LineEdit lineEdit;
	[Export] private OptionButton optionButton;

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
	/// Try to create a lobby with the name in lineEdit, and the player amount in optionButton.
	/// </summary>
	public void _on_button_down()
	{
		int selectedPlayer = optionButton.Selected + 2;
		SMuL.CreateLobby(lineEdit.Text, selectedPlayer);

		//Tell the main menu to start checking for if/when the lobby is created.
		mainMenuUi.StartChecking();
	}
}
