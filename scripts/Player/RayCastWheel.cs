using Godot;
using System;

[GlobalClass]
public partial class RayCastWheel : RayCast3D
{
	[Export]
	public float springStrength = 32000.0f;
	[Export]
	public float springDamping = 3577.0f;
	[Export]
	public float restDist = 0.2f;
	[Export]
	public float wheelRadius = 0.32f;
	[Export]
	public bool isMotor = false;
	[Export]
	public bool isSteer = false;

	public Node3D wheel => GetChild<Node3D>(0);  // Gets it when needed
}
