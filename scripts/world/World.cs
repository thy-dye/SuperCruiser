 using Godot;
using System;
public partial class World : Node
{
	//UI and timers
    [Export] public Control gameover;
    [Export] public Control ui;
	//Timer Stuff
    [Export] public Timer T;
	[Export] public Area3D goalArea;		
	[Export] public Node markerParent;

	private Godot.Collections.Array<Node3D> markers = new Godot.Collections.Array<Node3D>();
    private RandomNumberGenerator rng = new RandomNumberGenerator();
    private int timePerArea = 30;
	private int prevIndex = 0;
    private double timeElapsed = 0.0;
    private float currentScore = 0.0f; // Changed to float

    // Caching references for performance
    private Label timerLabel;
    private Label scoreLabel;

    public override void _Ready()
    {
        gameover = GetNode<Control>("GameOver");
        gameover.Visible = false;
        
        // Find the labels inside the UI PackedScene/Node
        timerLabel = ui.GetNode<Label>("Timer");
        scoreLabel = ui.GetNode<Label>("Score");

        Input.SetMouseMode(Input.MouseModeEnum.Captured);

		rng.Randomize();

		foreach (Node child in markerParent.GetChildren()) {
			if (child is Node3D marker) { markers.Add(marker); }
		}
		MoveAreaToRandomLocation();
    }

    public override void _Process(double delta)
    {
        // 1. Update Score: 100 points per second
        currentScore += (float)delta * 100.0f;

        // 2. Track total time for the difficulty scaling
        timeElapsed += delta;
        if (timeElapsed > 15.0)
        {
            timeElapsed = 0.0;
            timePerArea = Mathf.Max(5, timePerArea - 5); // Don't let it go below 5
			GD.Print(timePerArea);
        }

        // 3. Update the UI Text
        // We use (int) to hide the decimals from the player
        timerLabel.Text = $"Time: {Mathf.Round(T.TimeLeft)}";
        scoreLabel.Text = $"Score: {(int)currentScore}";
    }

	public void MoveAreaToRandomLocation()
	{
		if (markers.Count == 0) { return; }

		int index = rng.RandiRange(0, markers.Count - 1);
		if (prevIndex == index) { MoveAreaToRandomLocation(); }
		else
		{
			Node3D selectedMarker = markers[index];			
			goalArea.GlobalPosition = selectedMarker.GlobalPosition;
		}
	}

    private void OnDead(Vector3 pos)
    {
		SetProcess(false);
        gameover.Visible = true;
        Input.SetMouseMode(Input.MouseModeEnum.Visible);
        T.Stop();

		gameover.GetNode<Button>("PlayAgain").GrabFocus();
    }

    private void OnAreaEntered(Node3D body)
    {
		float newTime = (float)T.TimeLeft + timePerArea;
		T.WaitTime = newTime;
		T.Start();
		MoveAreaToRandomLocation();
    }
}