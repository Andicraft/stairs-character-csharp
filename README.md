# Stairs Character

A simple to use class that enables your CharacterBody3D to handle stairs properly.

## Usage instructions:

1. Make your character controller extend `StairsCharacter` instead of `CharacterBody3D`.
2. Call `HandleStairs()` before calling `MoveAndSlide()`.
3. Done!


If your controller uses multiple colliders, make sure the one closest to the ground is the first in the list. The C# version currently assumes the collider's shape is a `CapsuleShape3D`, but that should be easy to change in the code if you need it to use a different shape.
