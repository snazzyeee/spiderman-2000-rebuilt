# Menu contract and constrained swing

MainMenuManager requires exactly five configured buttons and sets their labels
and routing in Game, Options, Training, Special, Gallery order. New Input System
UI actions handle navigation/submit; the explicit cancel action handles Escape
and gamepad East. Both submit and cancel route to the configured original menu
confirmation sample. The Back row navigates without playing that sample twice.
SfxPlayed exposes the actual audio-source dispatch for validation/telemetry.

The player rigidbody remains kinematic with built-in gravity disabled. Swing
anchors must come from a collider raycast, be within AnchorRange, meet
AnchorMinimumHeight, and have an unobstructed rope path from the player's chest.
The selector respects the encounter arena and ignores trigger colliders.

SwingSolver projects the chest onto a sphere of the captured rope length every
substep. Both inward and outward radial velocity are removed. Tangential gravity,
trough boost, steering, damping and the speed cap then advance the pendulum;
the next position is projected back onto the same sphere and velocity onto its
tangent plane. Render-frame motion is subdivided at up to 120 Hz.

The swept box motor resolves building collisions before committing movement.
If collision changes the pendulum displacement by more than 2 mm, swinging
ends with the collision-adjusted world velocity. It does not teleport the player
back onto the rope sphere through an obstacle. Disabled/destroyed anchors, rope
occlusion, grounding, release input or jump also detach. Regular airborne
movement resumes with the release velocity; there are no SpringJoint or
dynamic-rigidbody forces competing with the motor.

Tests cover fixed length, zero radial velocity, tangent preservation, sustained
kinematic traversal, invalid/low/disabled anchors, wall collision and release
momentum. These constraints remove the former slack-rope state; they do not
establish original-game timing or guarantee the absence of every traversal bug.
