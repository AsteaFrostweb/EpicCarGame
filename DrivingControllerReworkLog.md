# Driving Controller Rework Log

Date: 2026-05-15

## Intention

Rework `Assets/SimpleWheelCarController.cs` back into a clear, tunable physics-first controller aimed at an NFS-style arcade feel.

The old version had too many systems affecting the same outcome at the same time. High-speed handling was especially hard to reason about because steering limits, auto drift, counter-steer, recovery steering, recovery braking, torque limiting, traction control, and dynamic grip recovery could all activate together.

The new version keeps Unity `Rigidbody` and `WheelCollider` physics as the base, then adds a small number of explicit arcade layers.

## Main Patch

Changed:

- `Assets/SimpleWheelCarController.cs`
- `Assets/Scripts/Audio/CarSfxController.cs`

Added:

- `DrivingControllerReworkLog.md`

## Follow-Up Patch: Car Rename Bug

`CarSfxController.CreateSource` used to add `AudioSource` components directly to the car and then assign `source.name`.

In Unity, setting `name` on a component can rename the owning GameObject. Because the one-shot source was created last, the car could appear renamed to `Car SFX One Shots` during play.

Changed `CreateSource` so audio sources now live on child GameObjects:

- `Car SFX Sources`
  - `Engine Loop`
  - `Acceleration Loop`
  - `Brake Loop`
  - `Skid Loop`
  - `Car SFX One Shots`

This keeps the hierarchy readable without mutating the car object's name.

## Follow-Up Patch: High-Speed NFS Turning

The first simplified baseline still felt too wheel-physics-only at high speed. That made the car reluctant to turn, which is the opposite of the target NFS feel.

Changed `Assets/SimpleWheelCarController.cs` again:

- Increased high-speed steering curve strength:
  - old max-speed steering scale: `0.42`
  - new max-speed steering scale: `0.72`
- Increased `downforce` from `18` to `35`.
- Increased normal grip:
  - `frontForwardGrip`: `1.45` to `1.55`
  - `rearForwardGrip`: `1.35` to `1.45`
  - `frontSidewaysGrip`: `1.35` to `1.75`
  - `rearSidewaysGrip`: `1.28` to `1.5`
- Added a separate `Turn Assist` section:
  - `normalTurnYawTorque`
  - `turnAssistMinSpeed`
  - `turnAssistFullSpeed`
  - `driftTurnAssistMultiplier`

The important change is `normalTurnYawTorque`. It applies controlled yaw torque while steering, scaled by speed. This is intentionally arcade-like: at higher speeds the car no longer depends only on front tyre steering angle to rotate.

If the car still will not turn enough, increase `normalTurnYawTorque` first. If it turns but feels twitchy, lower `normalTurnYawTorque` or lower the final key of `steerBySpeed`.

## Follow-Up Patch: Yaw Stability

The high-speed turn assist made the car rotate too easily. Light steering taps could build enough yaw rate to force a 180/360 with no recovery path.

Changed `Assets/SimpleWheelCarController.cs`:

- Reduced `normalTurnYawTorque` from `24` to `10`.
- Moved `turnAssistFullSpeed` from `42` to `55` so turn assist ramps in more gradually.
- Added `maxYawRate` to cap runaway spin speed.
- Added `yawDamping` to resist uncontrolled rotation at speed.
- Added `stabilityAssist` to nudge the car back toward its velocity direction when not drifting.

These controls are meant to separate "the car turns when I ask" from "the car spins forever once yaw starts."

## Follow-Up Patch: Ground-Aware Downforce

The car felt too stuck to the ground and behaved unnaturally when airborne.

Changed `Assets/SimpleWheelCarController.cs`:

- Reduced `downforce` from `35` to `8`.
- Added `groundedDownforceFadeSpeed`.
- Added `groundedAmount`, driven by the ratio of grounded wheels.
- Downforce now multiplies by `groundedAmount`, so it fades out when the car leaves the ground.

This keeps a small amount of high-speed stability on track, but stops downforce from dominating jumps.

## Follow-Up Patch: Steering Wobble Reduction

The car felt good overall, but it was too easy to wobble left and right at speed.

Changed `Assets/SimpleWheelCarController.cs`:

- Added `steeringDeadzone`.
- Added `steeringInputExponent`.
- Added `ShapeSteeringInput` so tiny A/D taps are softened instead of going straight into steering/yaw assist.
- Reduced `normalTurnYawTorque` from `10` to `8`.
- Increased `yawDamping` from `5` to `7`.
- Added `straightLineYawDamping` to settle yaw when steering input is near center.
- Reduced `stabilityAssist` from `7` to `4` so it does not overcorrect as aggressively.

This should make the car less prone to left-right oscillation while preserving the NFS-style turn authority.

## Follow-Up Patch: Camera Speed Framing

Added speed-based camera pullback and height offset in `Assets/CameraFollow.cs`.

Each camera style now has:

- `speedForMaxOffset`
- `extraDistanceAtMaxSpeed`
- `extraHeightAtMaxSpeed`

The camera uses planar rigidbody speed to blend from the normal follow position to a higher and further-back position. This should make high-speed driving easier to read because the player can see further down the road.

## New Controller Shape

The controller is now organized into these sections:

- Wheel references
- Core drive
- Input feel
- Speed curves
- Rigidbody setup
- Normal grip
- Drift
- Wheel collider defaults
- Telemetry and audio-facing public values

Runtime order in `FixedUpdate` is now:

1. `UpdateTelemetry`
2. `SmoothInputs`
3. `UpdateDriftAmount`
4. `ApplyWheelFriction`
5. `ApplyMotor`
6. `ApplySteering`
7. `ApplyBrakes`
8. `ApplyArcadeForces`

This order is deliberate: read physics first, decide state, apply tyre setup, then drive/steer/brake, then add small arcade forces.

## Systems Removed

Removed from the main behavior:

- Auto drift activation based on steering and slip angle
- Traction-control torque limiter
- Recovery assist calculation
- Recovery steering override
- Recovery rear grip boost
- Selective front-wheel recovery braking
- Drift front sideways boost
- Multi-threshold high-speed steering formula
- Separate steer acceleration/return angle system

Reason: these systems were individually sensible but collectively too tangled. At high speed, several of them could fight the player's input and each other.

## Systems Kept

Kept:

- Unity `Rigidbody`
- Unity `WheelCollider`
- Rear-wheel drive torque
- Brake and handbrake torque
- Wheel mesh pose updates
- Wheel collider default setup
- Slip telemetry for audio/debug
- Public audio-facing values like `Throttle01`, `WheelSpin01`, `Skid01`, and `Drift01`
- `ApplyArcadeDriftDefaults` as a compatibility alias for existing editor code

## New Systems Added

### Speed Curves

Replaced several speed-related formulas with two curves:

- `steerBySpeed`
- `torqueBySpeed`

Both evaluate from `Speed01`, where `0` is stopped and `1` is `maxSpeed`.

This should be much easier to tune than several linked speed thresholds. If high speed feels bad, start with `steerBySpeed`.

### Simple Drift State

Drift is now deliberate:

- `enableDrift`
- must be above `driftEnterSpeed`
- must be holding handbrake
- must have some steering input

While drifting, only a few things change:

- rear sideways grip moves toward `driftRearSidewaysGrip`
- rear forward grip moves toward `driftRearForwardGrip`
- steering gets `driftSteerMultiplier`
- throttle gets `driftThrottleMultiplier`
- optional yaw torque is applied by `driftYawTorque`
- optional mild counter-steer uses `counterSteerAssist`

No automatic drift entry and no recovery override are currently active.

### Arcade Forces

Added a small, explicit force layer:

- `downforce`
- `extraGravity`
- `driftYawTorque`

These are separated so it is clear which values are physics stabilization and which are drift rotation.

## Important Compatibility Notes

`ApplyArcadeDriftDefaults` still exists because `Assets/Editor/CenterOfGravityVisualizerEditor.cs` calls it. It now delegates to `ApplyNfsBaselineDefaults`.

The SFX adapter should still compile because the controller still exposes:

- `Throttle01`
- `Reverse01`
- `Brake01`
- `Handbrake01`
- `Speed01`
- `ForwardSpeed01`
- `Steering01`
- `EngineLoad01`
- `WheelSpin01`
- `Skid01`
- `Drift01`
- `RearForwardSlip`
- `RearSidewaysSlip`

`RecoveryAssist` remains as a public property returning `0f` so anything reading it does not break, but the recovery system itself has been removed.

## Suggested Tuning Order

Tune in this order. Do not tune everything at once.

1. Disable drift by setting `enableDrift = false`.
2. Tune `motorTorque`, `maxSpeed`, and `torqueBySpeed` until acceleration and top speed feel right.
3. Tune `steerBySpeed` until the car can turn at high speed without feeling twitchy or dead.
4. Tune normal grip values:
   - `frontSidewaysGrip`
   - `rearSidewaysGrip`
   - `frontForwardGrip`
   - `rearForwardGrip`
5. Tune `downforce` if the car gets floaty at speed.
6. Re-enable drift.
7. Tune drift grip:
   - `driftRearSidewaysGrip`
   - `driftRearForwardGrip`
8. Tune drift feel:
   - `driftSteerMultiplier`
   - `driftThrottleMultiplier`
   - `driftYawTorque`
   - `counterSteerAssist`

## First Values To Try If It Still Feels Bad

If high speed still refuses to turn:

- Increase the last key of `steerBySpeed` from `0.42` toward `0.55`.
- Increase `frontSidewaysGrip`.
- Increase `downforce`.

If high speed is twitchy:

- Lower the last key of `steerBySpeed`.
- Lower `steerResponse`.
- Increase `rearSidewaysGrip`.

If drift spins too easily:

- Increase `driftRearSidewaysGrip`.
- Lower `driftYawTorque`.
- Lower `driftSteerMultiplier`.
- Increase `counterSteerAssist` slightly, but avoid going too high.

If drift will not rotate:

- Lower `driftRearSidewaysGrip`.
- Increase `driftYawTorque`.
- Increase `handbrakeTorque`.

## Rationale

The point of this patch is not to magically finish the handling in one pass. It is to make the system small enough that tuning becomes possible by hand.

The new controller has fewer hidden interactions:

- Speed behavior lives mostly in curves.
- Normal driving uses fixed grip.
- Drift is a visible state.
- Arcade forces are explicit.
- Recovery automation has been removed.

That should make each test drive more informative.
