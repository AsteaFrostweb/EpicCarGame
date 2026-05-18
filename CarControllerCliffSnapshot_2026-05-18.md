# Car Controller Cliff Map Snapshot

Date: 2026-05-18

Purpose: restore point before experimenting with continuous axle slip based wheel friction.

## Source

- Scene checked: `Assets/Scenes/cliff map.unity`
- Prefab checked: `Assets/Prefabs/Car New.prefab`
- The cliff map scene uses `Assets/Prefabs/Car New.prefab` with prefab instance overrides.
- Scene overrides found:
  - `maxSpeed`: 32
  - `driftRearForwardGrip`: 0.86

## Core Drive

- `motorTorque`: 2400
- `brakeTorque`: 6200
- `handbrakeTorque`: 3600
- `maxSpeed`: 32 in `cliff map.unity`, overriding prefab value 50
- `reverseTorqueMultiplier`: 0.45
- `brakeResponseSpeed`: 2.25

## Input Feel

- `throttleResponse`: 8
- `steerResponse`: 10
- `steerReturnResponse`: 14
- `steeringDeadzone`: 0.08
- `steeringInputExponent`: 1.35

## Speed Curves

- `maxSteerAngle`: 36
- `steerBySpeed`:
  - `(0, 1)`
  - `(0.45, 0.9)`
  - `(1, 0.72)`
- `torqueBySpeed`:
  - `(0, 1)`
  - `(0.7, 0.9)`
  - `(1, 0.12)`

## Rigidbody Setup

- `applyCenterOfMassOffsetOnAwake`: false
- `centerOfMassOffset`: `(0, -0.45, -0.08)`
- `downforce`: 8
- `extraGravity`: 0
- `groundedDownforceFadeSpeed`: 10

## Turn Assist

- `normalTurnYawTorque`: 10
- `turnAssistMinSpeed`: 8
- `turnAssistFullSpeed`: 55
- `driftTurnAssistMultiplier`: 0.35
- `maxYawRate`: 1.6
- `yawDamping`: 5
- `straightLineYawDamping`: 12
- `stabilityAssist`: 7

## Normal Grip

- `frontForwardGrip`: 1.55
- `rearForwardGrip`: 1.45
- `frontSidewaysGrip`: 1.75
- `rearSidewaysGrip`: 1.5

## Drift

- `enableDrift`: true
- `driftEnterSpeed`: 8
- `driftBuildSpeed`: 6
- `driftRecoverSpeed`: 4
- `driftRearSidewaysGrip`: 0.76
- `driftRearForwardGrip`: 0.86 in `cliff map.unity`, overriding prefab value 1.05
- `driftSteerMultiplier`: 1.18
- `driftThrottleMultiplier`: 0.78
- `driftYawTorque`: 8
- `counterSteerAssist`: 0.18

## Wheel Collider Defaults

- `wheelMass`: 26
- `wheelDampingRate`: 0.9
- `suspensionDistance`: 0.28
- `forceAppPointDistance`: 0.08
- `suspensionSpring`: 36000
- `suspensionDamper`: 5200
- `suspensionTargetPosition`: 0.5

## Wheel Friction Curve Shape

Runtime `ApplyFrictionToWheel` currently writes these curve values:

- Forward friction:
  - `extremumSlip`: 0.34
  - `extremumValue`: 1
  - `asymptoteSlip`: 0.82
  - `asymptoteValue`: 0.78
- Sideways friction:
  - `extremumSlip`: 0.26
  - `extremumValue`: 1
  - `asymptoteSlip`: 0.72
  - `asymptoteValue`: 0.72

The stiffness values are supplied from the grip values above.

## Worktree Note

At snapshot time, these unrelated untracked files already existed:

- `Assets/Scripts/Car.cs`
- `Assets/Scripts/Car.cs.meta`
- `Assets/Scripts/Objects.meta`
