using Godot;

public partial class StairsCharacter : CharacterBody3D
{
	[ExportCategory("Stair Stepping")]
	[Export] protected float _stepHeight = 0.33f;

	// Holds the margin from the player's Collider.
	private float _colliderMargin;

	// Use WasGrounded instead of IsOnFloor() - because of the stair step mechanism, sometimes this
	// script will snap the player to the floor, but IsOnFloor() will still read as false.
	public bool Grounded;
	public bool WasGrounded;
	
	// Force a stair step check this frame
	// I use this for things like wall jumps, where it feels like you _should've_ been able to land on a ledge but
	// snagged just below it.
	public bool ForceStairStep;

	// Similarly, you can modify this and it will reset after the frame.
	// If set, will be used in place of _stepHeight.
	public float TempStepHeight = 0f;
	
	// Used to filter out the Y velocity from the player when checking stair steps.
	private readonly Vector3 _horizontal = new Vector3(1, 0, 1);

	// DesiredVelocity should be set in your character controller just so we know where we _want_ to go.
	// Gets reset at the start of every frame - should match the direction where your input wants to take you.
	protected Vector3 DesiredVelocity = Vector3.Zero;
	
	public override void _Ready()
	{
		base._Ready();
		
		// Only requirement for your Player scene is that your collider is named Collider.
		// Your margin should be set real low or it starts snagging on everything - 0.001 works for me.
		_colliderMargin = GetNode<CollisionShape3D>("Collider").Shape.Margin;
		
		if (_colliderMargin > 0.01f)
			GD.PushWarning("Margin on player's collider shape is over 0.01, may snag on stair steps");
	}

	public override void _PhysicsProcess(double delta)
	{
		WasGrounded = Grounded;
		Grounded = IsOnFloor();
		DesiredVelocity = Vector3.Zero;
	}
	public void MoveAndStairStep()
	{
		StairStepUp();
		MoveAndSlide();
		StairStepDown();
	}
	protected void StairStepDown()
	{
		// Not on the ground last stair step, or currently jumping? Don't snap to the ground
		// Prevents from suddenly snapping when you're falling
		if (WasGrounded == false || Velocity.Y >= 0) return;
		
		var result = new PhysicsTestMotionResult3D();
		var parameters = new PhysicsTestMotionParameters3D();

		parameters.From = GlobalTransform;
		parameters.Motion = Vector3.Down * _stepHeight;
		parameters.Margin = _colliderMargin;

		if (!PhysicsServer3D.BodyTestMotion(GetRid(), parameters, result)) return;
		
		GlobalTransform = GlobalTransform.Translated(result.GetTravel());
		ApplyFloorSnap();
	}

	protected void StairStepUp()
	{
		// Skip stair step if in the air (unless forced)
		if (!Grounded && ForceStairStep == false) return;
		
		var horizontalVelocity = Velocity * _horizontal;
		var testingVelocity = horizontalVelocity;

		if (horizontalVelocity == Vector3.Zero) 
			testingVelocity = DesiredVelocity;
			
		// Not moving or attempting to move, skip stair check
		if (testingVelocity == Vector3.Zero) return;

		var result = new PhysicsTestMotionResult3D();
		var parameters = new PhysicsTestMotionParameters3D();

		// Transform gets reused for every check
		var transform = GlobalTransform;
		
		// Fun fact: You don't need to pass 'delta' everywhere if you just wanna use this instead.
		var distance = testingVelocity * (float)GetPhysicsProcessDeltaTime();
		parameters.From = transform;
		parameters.Motion = distance;
		parameters.Margin = _colliderMargin;

		// No stair step to bother with because we're not hitting anything
		if (PhysicsServer3D.BodyTestMotion(GetRid(), parameters, result) == false)
			return;

		//Move to collision
		var remainder = result.GetRemainder();
		transform = transform.Translated(result.GetTravel());

		// Raise up to ceiling - can't walk on steps if there's a low ceiling
		var stepUp = _stepHeight * Vector3.Up;
		parameters.From = transform;
		parameters.Motion = stepUp;
		PhysicsServer3D.BodyTestMotion(GetRid(), parameters, result);
		transform = transform.Translated(result.GetTravel()); // GetTravel will be full length if we didn't hit anything
		var stepUpDistance = result.GetTravel().Length();

		// Move forward remaining distance
		parameters.From = transform;
		parameters.Motion = remainder;
		PhysicsServer3D.BodyTestMotion(GetRid(), parameters, result);
		transform = transform.Translated(result.GetTravel());
		
		// And set the collider back down again
		parameters.From = transform;
		// But no further than how far we stepped up
		parameters.Motion = Vector3.Down * stepUpDistance;
		
		//Don't bother with the rest if we're not actually gonna land back down on something
		if (PhysicsServer3D.BodyTestMotion(GetRid(), parameters, result) == false)
			return;
		
		transform = transform.Translated(result.GetTravel());
		
		var surfaceNormal = result.GetCollisionNormal();
		if (surfaceNormal.AngleTo(Vector3.Up) > FloorMaxAngle) return; //Can't stand on the thing we're trying to step on anyway
		
		var gp = GlobalPosition;
		gp.Y = transform.Origin.Y;
		GlobalPosition = gp;
	}
}
