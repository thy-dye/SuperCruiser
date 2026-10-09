using Godot;
using System;

public partial class Player : RigidBody3D
{
	[Signal]
	public delegate void DeadEventHandler(Vector3 position);
	//acceleration and wheels
	[Export]
	public Godot.Collections.Array<RayCastWheel> wheels { get; set; }
	[Export]
	public float acceleration = 20.0f;
	[Export]
	public float deceleartion = 10.0f;
	[Export]
	public float maxSpeed = 45.0f;
	[Export]
	public float gripFactor = 0.9f; 
	[Export]
	public float drag = 7.0f;


	//camera variables
	[Export]
	public Node3D cameraPivot;
	[Export]
	public Camera3D camera;	
	[Export]
	public Camera3D reverseCamera;
	private bool isFacingForward = true;
	private Vector3 lookAt;
	private float regularFov = 75.0f;
	private float speedFov = 120.0f;

	//Steering
	[Export]

	public float steerAngle = Mathf.Pi / 4.0f;
	[Export]

	public float steerSpeed = 0.1f;
	[Export]
	public float torqueStrength = 50;
	[Export]
	public float maxAngularVelocity = 1f;

	

	//other qualities
	[Export]
	public Node3D COM;

	[Export]
	public Node3D arrow;

	[Export]
	public Area3D area;
	//input handling variables
	public int motorInput = 0;
	public float steerInput = 0.0f;

	public override void _Ready()
	{
		foreach (Node child in GetChildren())
		{
			if (child is RayCastWheel rayCastWheel ) {  wheels.Add(rayCastWheel); } 
		}
		cameraPivot = GetNode<Node3D>("CameraPivot");
		camera = GetNode<Camera3D>("CameraPivot/Camera3D");
		reverseCamera = GetNode<Camera3D>("CameraPivot/ReverseCamera");
		COM = GetNode<Node3D>("COM");
		lookAt = GlobalPosition;
		Input.SetMouseMode(Input.MouseModeEnum.Captured);
	}

	public override void _PhysicsProcess(double delta)
	{
		steerInput = Input.GetAxis("right", "left"); 
    	motorInput = (int)Input.GetAxis("backward", "forward");
		
		bool grounded = false;

		foreach (RayCastWheel wheel in wheels) {
			if (wheel.IsColliding()) { grounded = true; }
			wheel.ForceRaycastUpdate();
			_SingleWheelSuspension(wheel);
			_SingleWheelAcceleration(wheel);
			_ApplyLateralFriction(wheel);
		}
		_ApplySteering(grounded);

		if (!grounded) {
			_AirTrick((float)delta);
			CenterOfMass = COM.Position + new Vector3(0, -3, 0); 
		}
		else { CenterOfMass = COM.Position; }
		// GD.Print(AngularVelocity + " " + LinearVelocity);

		//camera code
		_CameraRotation((float)delta);
		_CheckCameraSwitch();
	}

	public Vector3 _GetPointVelocity (Vector3 point)
	{
		return LinearVelocity + AngularVelocity.Cross(point - GlobalPosition);
	}

	public void _ApplyLateralFriction(RayCastWheel ray)
	{
		if (!ray.IsColliding()) return;

		Vector3 contactPoint = ray.GetCollisionPoint();
		Vector3 wheelVelocity = _GetPointVelocity(contactPoint);
		Vector3 sideDir = ray.GlobalBasis.X; // Right vector
		Vector3 forwardDir = ray.GlobalBasis.Z; // Forward vector
		
		// Calculate sideways velocity
		float lateralVelocity = sideDir.Dot(wheelVelocity);
		
		// Kill sideways movement based on grip
		float frictionForce = -lateralVelocity * Mass * gripFactor;
		Vector3 frictionVector = sideDir * frictionForce;
		
		// Apply at contact point
		ApplyForce(frictionVector, contactPoint - GlobalPosition);
		
		// EXTRA DAMPING FOR SPINNING - THIS IS KEY
		// If the car is spinning, dampen that too
		float spinRate = AngularVelocity.Y; // Y-axis rotation
		if (motorInput == 0 && Mathf.Abs(spinRate) > 0.01f)
		{
			// Counter-torque to stop spinning
			float antiSpinTorque = -spinRate * Mass * gripFactor * 2f;
			ApplyTorque(Vector3.Up * antiSpinTorque);
		}
	}
	public void _SingleWheelAcceleration (RayCastWheel ray)
	{
		Vector3 forwardDir = ray.GlobalBasis.Z; //backwards (our car is facing backwards I dont want to touch it icanse it breaks)
		float vel = forwardDir.Dot(LinearVelocity);

		ray.wheel.RotateX((vel * (float)GetProcessDeltaTime()) / ray.wheelRadius);
		
		if (ray.IsColliding())
		{
			Vector3 contact = ray.wheel.GlobalPosition;
			Vector3 forcePos = contact - GlobalPosition;
			//driving portion
			if (ray.isMotor && motorInput != 0 && Math.Abs(vel) < maxSpeed)
			{
				Vector3 forceVector = forwardDir * acceleration * Mass * motorInput;
				ApplyForce(forceVector, forcePos);		
			}

			//deceleration
			if (Mathf.Abs(vel) > 0.001 && motorInput == 0) {
				Vector3 dragForceVector = GlobalBasis.Z * drag * Mass * Mathf.Sign(-vel);
				ApplyForce(dragForceVector, forcePos);
			}
		}
	}
	public void _SingleWheelSuspension (RayCastWheel ray)
	{ 
		if (ray.IsColliding())
		{			
			ray.TargetPosition = new Vector3(0, -(ray.restDist + ray.wheelRadius + 0.5f), 0);
			Vector3 contact = ray.GetCollisionPoint();
			Vector3 springUpDir = ray.GlobalTransform.Basis.Y;
			float springLen = ray.GlobalPosition.DistanceTo(contact) - ray.wheelRadius;
			float offset = ray.restDist - springLen;

			Node3D wheel = ray.GetNode<Node3D>("Wheel");
			wheel.Position = new Vector3(wheel.Position.X, -springLen, wheel.Position.Z);

			float springForce = ray.springStrength * offset;

			// damping force = damping * relative velocity
			Vector3 worldVel = _GetPointVelocity(contact);
			float relativeVel = springUpDir.Dot(worldVel);
			float springDampForce = ray.springDamping * relativeVel;


			Vector3 forceVector = (springForce - springDampForce) * ray.GetCollisionNormal();

			contact = ray.wheel.GlobalPosition;
			Vector3 forcePosOffset = contact - GlobalPosition;
			ApplyForce(forceVector, forcePosOffset);
		}
	}

	public void _ApplySteering(bool grounded)
	{
		if (!grounded) return;

		float forwardVel = GlobalBasis.Z.Dot(LinearVelocity);
		float currentAngVelY = GlobalBasis.Y.Dot(AngularVelocity);

		if (steerInput != 0 && (Mathf.Abs(forwardVel) > 1.0f || motorInput != 0))
		{

			if (Mathf.Abs(currentAngVelY) < maxAngularVelocity)
			{
				float steerTorque = torqueStrength * Mass * steerInput;
				ApplyTorque(GlobalBasis.Y * steerTorque);
			}
		}
		else if (steerInput == 0)
		{
			// 2. STEERING STABILIZATION (The Fix)
			// When there's no input, apply a counter-torque to stop the spin
			// This acts like the self-centering force of a real car
			float stabilizationStrength = 25.0f; // Adjust this to feel "snappier"
			float counterTorque = -currentAngVelY * stabilizationStrength * Mass;
			
			ApplyTorque(GlobalBasis.Y * counterTorque);
		}
	}
	public void _CameraRotation(float delta)
	{
		// 1. Dampen the Pivot movement
		cameraPivot.GlobalPosition = cameraPivot.GlobalPosition.Lerp(GlobalPosition, delta * 15.0f);
		
		// 2. Smoother Transform interpolation
		cameraPivot.Transform = cameraPivot.Transform.InterpolateWith(Transform, delta * 5.0f);

		// 3. Velocity Thresholding (The Jitter Killer)
		Vector3 effectiveVelocity = LinearVelocity;
		if (effectiveVelocity.Length() < 0.5f) effectiveVelocity = Vector3.Zero;

		// 4. Smooth out the 'LookAt' target
		Vector3 targetLookAt = GlobalPosition + (effectiveVelocity * 0.5f);
		lookAt = lookAt.Lerp(targetLookAt, delta * 5.0f);
		
		camera.LookAt(lookAt);
		reverseCamera.LookAt(lookAt);

		// 5. FOV Smoothing
		float speed = LinearVelocity.Length(); 
		if (speed < 0.5f) speed = 0; // Don't change FOV for tiny movements
		
		float t = Mathf.Clamp(speed / maxSpeed, 0.0f, 1.0f);
		float targetFov = Mathf.Lerp(regularFov, speedFov, t);
		
		camera.Fov = Mathf.Lerp(camera.Fov, targetFov, delta * 2.0f);
		reverseCamera.Fov = Mathf.Lerp(reverseCamera.Fov, targetFov, delta * 2.0f);

		//area look at for the arrow 
		Vector3 target = area.GlobalPosition;
		arrow.LookAt(target, Vector3.Up);
	}
	public void _CheckCameraSwitch()
	{
		float forwardSpeed = LinearVelocity.Dot(Transform.Basis.Z);
		if (isFacingForward)
		{
			// If we are moving backward faster than 1.5 m/s, switch to reverse
			if (forwardSpeed < -1.5f) 
			{
				isFacingForward = false;
				camera.Current = false;
				reverseCamera.Current = true;
			}
		}
		else
		{
			// If we are moving forward faster than 0.5 m/s, switch back to front
			if (forwardSpeed > 0.5f)
			{
				isFacingForward = true;
				camera.Current = true;
				reverseCamera.Current = false;
			}
		}
	}
	
	public void _AirTrick(float delta)
	{
		Vector3 currentUp = GlobalBasis.Y;
		Vector3 targetUp = Vector3.Up;
		Vector3 correctionAxis = currentUp.Cross(targetUp);

		float errorAngle = currentUp.AngleTo(targetUp);
		float levelingStrength = 300.0f; 
		
		Vector3 torque = correctionAxis * errorAngle * levelingStrength * Mass;
		
		ApplyTorque(torque);
		AngularVelocity = AngularVelocity.Lerp(Vector3.Zero, delta * 2.0f);
	}

	public void _Die()
	{
		camera.Current = false;
		reverseCamera.Current = false;
		EmitSignal(SignalName.Dead, GlobalPosition);
		QueueFree();
	}
	private void OnTimeOut()
	{
		_Die();
	}

	private void OnOceanEntered(Node3D body)
	{
		_Die();
	}
}
