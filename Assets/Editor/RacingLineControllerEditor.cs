using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(RacingLineController))]
public class RacingLineControllerEditor : Editor
{
    private enum EditMode
    {
        Off,
        Append,
        Insert
    }

    private static EditMode editMode;

    private RacingLineController RacingLine => (RacingLineController)target;

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Scene Editing", EditorStyles.boldLabel);

        using (new EditorGUILayout.HorizontalScope())
        {
            DrawModeButton("Off", EditMode.Off);
            DrawModeButton("Append", EditMode.Append);
            DrawModeButton("Insert", EditMode.Insert);
        }

        EditorGUILayout.HelpBox(
            "Append/Insert mode: right-click or Shift+left-click in the Scene view. Clicks raycast against colliders, then fall back to the track root height.",
            MessageType.Info);

        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("Append Suggested Point"))
            {
                Undo.RecordObject(RacingLine, "Append Racing Line Point");
                RacingLine.AppendPoint(RacingLine.GetSuggestedAppendWorldPosition());
                EditorUtility.SetDirty(RacingLine);
                SceneView.RepaintAll();
            }

            if (GUILayout.Button("Rebuild Samples"))
            {
                RacingLine.RebuildSamples();
                SceneView.RepaintAll();
            }
        }

        using (new EditorGUI.DisabledScope(RacingLine.ControlPointCount == 0))
        {
            if (GUILayout.Button("Remove Last Point"))
            {
                Undo.RecordObject(RacingLine, "Remove Racing Line Point");
                RacingLine.RemovePoint(RacingLine.ControlPointCount - 1);
                EditorUtility.SetDirty(RacingLine);
                SceneView.RepaintAll();
            }
        }

        EditorGUILayout.LabelField("Control Points", RacingLine.ControlPointCount.ToString());
        EditorGUILayout.LabelField("Sampled Points", RacingLine.SampleCount.ToString());
    }

    private void OnSceneGUI()
    {
        RacingLineController racingLine = RacingLine;
        DrawPointLabels(racingLine);

        if (editMode != EditMode.Off)
        {
            DrawPointHandles(racingLine);
        }

        if (editMode == EditMode.Off)
        {
            return;
        }

        Event currentEvent = Event.current;
        bool placementClick =
            currentEvent.type == EventType.MouseDown &&
            ((currentEvent.button == 1 && !currentEvent.alt) ||
             (currentEvent.button == 0 && currentEvent.shift && !currentEvent.alt));

        if (!placementClick)
        {
            return;
        }

        Vector3 placementPosition = GetScenePlacementPosition(currentEvent.mousePosition, racingLine.transform.position.y);

        Undo.RecordObject(racingLine, editMode == EditMode.Append
            ? "Append Racing Line Point"
            : "Insert Racing Line Point");

        if (editMode == EditMode.Append)
        {
            racingLine.AppendPoint(placementPosition);
        }
        else
        {
            racingLine.InsertPointNearCurve(placementPosition);
        }

        EditorUtility.SetDirty(racingLine);
        currentEvent.Use();
        SceneView.RepaintAll();
    }

    private void DrawModeButton(string label, EditMode mode)
    {
        bool wasActive = editMode == mode;
        bool isActive = GUILayout.Toggle(wasActive, label, "Button");
        if (isActive != wasActive && isActive)
        {
            editMode = mode;
            SceneView.RepaintAll();
        }
    }

    private void DrawPointLabels(RacingLineController racingLine)
    {
        for (int i = 0; i < racingLine.ControlPointCount; i++)
        {
            Vector3 point = racingLine.GetControlPointWorld(i);
            Handles.Label(point + Vector3.up * 2f, $"{i}: {racingLine.GetDesiredSpeed(i):0}");
        }
    }

    private void DrawPointHandles(RacingLineController racingLine)
    {
        for (int i = 0; i < racingLine.ControlPointCount; i++)
        {
            Vector3 point = racingLine.GetControlPointWorld(i);
            EditorGUI.BeginChangeCheck();
            Vector3 movedPoint = Handles.PositionHandle(point, Quaternion.identity);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(racingLine, "Move Racing Line Point");
                racingLine.SetControlPointWorld(i, movedPoint);
                EditorUtility.SetDirty(racingLine);
            }
        }
    }

    private static Vector3 GetScenePlacementPosition(Vector2 mousePosition, float fallbackY)
    {
        Ray ray = HandleUtility.GUIPointToWorldRay(mousePosition);

        if (Physics.Raycast(ray, out RaycastHit hit, 5000f))
        {
            return hit.point;
        }

        Plane plane = new Plane(Vector3.up, new Vector3(0f, fallbackY, 0f));
        return plane.Raycast(ray, out float enter)
            ? ray.GetPoint(enter)
            : Vector3.zero;
    }
}
