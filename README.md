# Stairs Character

A simple to use class that enables your CharacterBody3D to handle stairs properly.

Mainly tested with the Jolt physics engine and cylinder colliders, not guaranteed to work well with anything else - but try it!

## Usage instructions:

1. Make your character controller extend `StairsCharacter` instead of `CharacterBody3D`.
2. Ensure your character's collider is named 'Collider'.
3. Every frame, set `DesiredVelocity` to the desired direction of movement.
4. Call `MoveAndStairStep()` instead of calling `MoveAndSlide()`.
5. Done!

### Important:

Ensure your character collider's margin value is set low - at most 0.01. Anything higher might cause snags. If you find that you're still snagging on ledges, lower it some more.