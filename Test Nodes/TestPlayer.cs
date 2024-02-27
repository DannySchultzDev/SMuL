using Godot;
using System;

public partial class TestPlayer : Node2D
{
	[Export] private int playerId;
	[Export] private Label label;

	public int pos { get; private set; } = 0;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		Move(0);
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}

	/// <summary>
	/// change the players physical and representational position.
	/// </summary>
	/// <param name="movement"></param>
	public void Move(int movement)
	{
		this.pos += movement;
		label.Text = "Player " + playerId + " is at " + pos + ".";
		Position += Vector2.Right * movement * 10;
	}
}
