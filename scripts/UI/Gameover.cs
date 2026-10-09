using Godot;
using Godot.Collections;
using System;

public partial class Gameover : Control
{
	[Export]
	public Label txt;
	private Array<String> str = new Array<String> {"WOMP WOMP" , "Git Gud", "Such a Travesty", "I'm too tired for this"};

    public override void _Ready()
    {
        txt = GetNode<Label>("Label");

		this.VisibilityChanged += OnVisibilityChanged;
    }

	private void OnVisibilityChanged()
	{
		if (Visible)
		{
			if (str.Count == 0) return;
			int randomIndex = (int)(GD.Randi() % str.Count);
			txt.Text = str[randomIndex];
		}
	}

	private void OnPlayAgainPressed() { GetTree().ChangeSceneToFile("res://scenes/world.tscn"); }
	private void OnMainMenuPressed() { GetTree().ChangeSceneToFile("res://scenes/UI/titlescreen.tscn"); }
}
