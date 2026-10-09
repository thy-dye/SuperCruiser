using Godot;
using System;

public partial class Main : Control
{
	private void OnStartPressed() { GetTree().ChangeSceneToFile("res://scenes/world.tscn"); }
	private void OnQuitPressed() { GetTree().Quit(); }
}
