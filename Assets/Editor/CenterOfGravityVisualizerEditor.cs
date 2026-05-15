using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(SimpleWheelCarController))]
public class SimpleWheelCarControllerEditor : Editor
{
    private const float SphereRadius = 0.25f;
    private static readonly Color FillColor = new Color(0.1f, 0.8f, 1f, 0.25f);
    private static readonly Color SelectedFillColor = new Color(0.1f, 0.8f, 1f, 0.45f);
    private static readonly Color WireColor = new Color(0f, 0.95f, 1f, 0.95f);

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        DrawDefaultInspector();

        SimpleWheelCarController car = (SimpleWheelCarController)target;

        EditorGUILayout.Space(8f);
        DrawSetupBox(car);
        DrawRuntimeInfo(car);
        DrawPresetButtons();

        serializedObject.ApplyModifiedProperties();
    }

    private void DrawSetupBox(SimpleWheelCarController car)
    {
        int missingColliders = CountMissingColliders(car);
        int missingMeshes = CountMissingMeshes(car);

        if (missingColliders > 0)
        {
            EditorGUILayout.HelpBox(
                $"Missing {missingColliders} WheelCollider reference(s). The car will not drive correctly until all four are assigned.",
                MessageType.Warning);
        }
        else
        {
            EditorGUILayout.HelpBox(GetWheelSummary(car), MessageType.Info);
        }

        if (missingMeshes > 0)
        {
            EditorGUILayout.HelpBox(
                $"Missing {missingMeshes} wheel mesh reference(s). Physics still works, but the visual wheels will not animate.",
                MessageType.None);
        }

        if (!car.applyCenterOfMassOffsetOnAwake)
        {
            EditorGUILayout.HelpBox(
                "Center of mass offset is only visualized. Enable Apply Center Of Mass Offset On Awake if you want this script to write it to the Rigidbody.",
                MessageType.None);
        }
    }

    private void DrawRuntimeInfo(SimpleWheelCarController car)
    {
        EditorGUILayout.Space(4f);
        EditorGUILayout.LabelField("Runtime Info", EditorStyles.boldLabel);

        using (new EditorGUI.DisabledScope(true))
        {
            EditorGUILayout.FloatField("Speed", car.speed);
            EditorGUILayout.FloatField("Forward Velocity", car.currentForwardVelocity);
            EditorGUILayout.FloatField("Drift Amount", car.DriftAmount);
            EditorGUILayout.FloatField("Recovery Assist", car.RecoveryAssist);
            EditorGUILayout.FloatField("Slip Angle", car.SlipAngle);
            EditorGUILayout.FloatField("Rear Forward Slip", car.RearForwardSlip);
            EditorGUILayout.FloatField("Rear Sideways Slip", car.RearSidewaysSlip);
            EditorGUILayout.Space(2f);
            EditorGUILayout.Slider("Audio Speed", car.Speed01, 0f, 1f);
            EditorGUILayout.Slider("Audio Throttle", car.Throttle01, 0f, 1f);
            EditorGUILayout.Slider("Audio Engine Load", car.EngineLoad01, 0f, 1f);
            EditorGUILayout.Slider("Audio Wheel Spin", car.WheelSpin01, 0f, 1f);
            EditorGUILayout.Slider("Audio Skid", car.Skid01, 0f, 1f);
            EditorGUILayout.Slider("Audio Drift", car.Drift01, 0f, 1f);
            EditorGUILayout.Toggle("Braking", car.isBraking);
            EditorGUILayout.Toggle("Handbraking", car.isHandbraking);
        }
    }

    private void DrawPresetButtons()
    {
        EditorGUILayout.Space(8f);
        EditorGUILayout.LabelField("Presets", EditorStyles.boldLabel);

        if (GUILayout.Button("Apply Arcade Drift Defaults"))
        {
            foreach (Object selectedTarget in targets)
            {
                SimpleWheelCarController selectedCar = (SimpleWheelCarController)selectedTarget;
                Undo.RecordObject(selectedCar, "Apply Arcade Drift Defaults");
                selectedCar.ApplyArcadeDriftDefaults();
                EditorUtility.SetDirty(selectedCar);
            }
        }

        if (GUILayout.Button("Apply Wheel Collider Defaults Only"))
        {
            foreach (Object selectedTarget in targets)
            {
                SimpleWheelCarController selectedCar = (SimpleWheelCarController)selectedTarget;
                Undo.RecordObject(selectedCar, "Apply Wheel Collider Defaults");
                selectedCar.ApplyWheelColliderDefaults();
                EditorUtility.SetDirty(selectedCar);
            }
        }
    }

    private static string GetWheelSummary(SimpleWheelCarController car)
    {
        return
            "WheelCollider driven arcade controller.\n" +
            $"FL {DescribeWheel(car.frontLeftCollider)} | FR {DescribeWheel(car.frontRightCollider)}\n" +
            $"RL {DescribeWheel(car.rearLeftCollider)} | RR {DescribeWheel(car.rearRightCollider)}";
    }

    private static string DescribeWheel(WheelCollider wheel)
    {
        if (wheel == null)
        {
            return "missing";
        }

        return $"mass {wheel.mass:0.#}, spring {wheel.suspensionSpring.spring:0}, damper {wheel.suspensionSpring.damper:0}";
    }

    private static int CountMissingColliders(SimpleWheelCarController car)
    {
        int missing = 0;

        if (car.frontLeftCollider == null) missing++;
        if (car.frontRightCollider == null) missing++;
        if (car.rearLeftCollider == null) missing++;
        if (car.rearRightCollider == null) missing++;

        return missing;
    }

    private static int CountMissingMeshes(SimpleWheelCarController car)
    {
        int missing = 0;

        if (car.frontLeftMesh == null) missing++;
        if (car.frontRightMesh == null) missing++;
        if (car.rearLeftMesh == null) missing++;
        if (car.rearRightMesh == null) missing++;

        return missing;
    }

    [DrawGizmo(GizmoType.NonSelected | GizmoType.Selected | GizmoType.Active)]
    private static void DrawCenterOfGravityGizmo(SimpleWheelCarController car, GizmoType gizmoType)
    {
        if (car == null)
        {
            return;
        }

        Vector3 worldCenter = GetWorldCenterOfGravity(car);
        bool selected = (gizmoType & GizmoType.Selected) != 0;

        Gizmos.color = selected ? SelectedFillColor : FillColor;
        Gizmos.DrawSphere(worldCenter, SphereRadius);

        Gizmos.color = WireColor;
        Gizmos.DrawWireSphere(worldCenter, SphereRadius);

        if (selected)
        {
            Handles.Label(worldCenter + Vector3.up * (SphereRadius + 0.05f), "Center of Gravity");
        }
    }

    private static Vector3 GetWorldCenterOfGravity(SimpleWheelCarController car)
    {
        Rigidbody rb = car.GetComponent<Rigidbody>();
        Vector3 localCenter = car.centerOfMassOffset;

        if (rb != null)
        {
            localCenter = Application.isPlaying && car.applyCenterOfMassOffsetOnAwake
                ? rb.centerOfMass
                : rb.centerOfMass + car.centerOfMassOffset;
        }

        return car.transform.TransformPoint(localCenter);
    }
}
