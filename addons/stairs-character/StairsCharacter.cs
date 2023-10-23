using Godot;

public partial class StairsCharacter : CharacterBody3D
{
	[ExportCategory("Stair Stepping")]
	[Export] private float _stepHeight = 0.33f;
	[Export] private float _stepMargin = 0.1f;

	private float _cylinderRadius = 0.5f;
	private CollisionShape3D _separator;
	private RayCast3D _rayCast;
	private float _rayShapeLocalHeight;
	
	public override void _Ready()
	{
		foreach (var node in GetChildren())
		{
			if (node is not CollisionShape3D col) continue;
		
			if (col.Shape is not CapsuleShape3D collider)
			{
				GD.PrintErr("StairCharacter's collider must use a CapsuleShape3D!");
				break;
			}
			
			// Create the separator node
			_separator = new CollisionShape3D();
			_separator.RotationDegrees = new Vector3(90, 0, 0);
			var shape = new SeparationRayShape3D();
			shape.Length = _stepHeight;
			_separator.Shape = shape;

			// Create raycast node (cheaper than raycasting from code)
			_rayCast = new RayCast3D();
			_rayCast.TargetPosition = Vector3.Down * _stepHeight;
			_rayCast.CollisionMask = CollisionMask;
			_rayCast.ExcludeParent = true;
			_rayCast.Enabled = false;

			_rayShapeLocalHeight = col.Position.Y - collider.Height * 0.5f + _stepHeight;
			_cylinderRadius = collider.Radius;
			AddChild(_separator);
			AddChild(_rayCast);
			_separator.TranslateObjectLocal(_rayShapeLocalHeight * Vector3.Down);
			
			break;
		}
	}

	/// <summary>
	/// Enables the character to walk up stairs. Call before MoveAndSlide().
	/// </summary>
	protected void HandleStairs()
	{
		if (IsOnFloor() == false || GetLastSlideCollision() == null)
		{
			_separator.Disabled = true;
			return;
		}

		var localPos = ToLocal(GetLastSlideCollision().GetPosition());
		localPos.Y = 0;

		var dir = (localPos * new Vector3(1, 0, 1)).Normalized();
		localPos += dir * _stepMargin;
		localPos = localPos.LimitLength(_cylinderRadius + _stepMargin);
		localPos.Y = _rayShapeLocalHeight;

		_rayCast.Position = localPos;
		_rayCast.ForceUpdateTransform();
		_rayCast.ForceRaycastUpdate();

		// Don't walk up stupid steep slopes
		var angle = _rayCast.GetCollisionNormal().AngleTo(UpDirection);
		if (angle > FloorMaxAngle)
		{
			return;
		}
		
		// The separator handles moving the character up the step
		_separator.Disabled = false;
		_separator.Position = localPos;
	}
}
