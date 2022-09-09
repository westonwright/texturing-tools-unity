using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public partial class DrawingSurfaceEditor : Editor
{
    /// <summary>
    /// Draw or do input/handle overriding in the scene view
    /// </summary>
    void OnSceneGUI()
    {
        if (!drawingMode) return;
        //Debug.Log(Tools.viewToolActive);
        // Event.current houses information on scene view input this cycle
        Event current = Event.current;
        if (Tools.viewToolActive)
        {
            if (pointerDown)
            {
                PointerUpActions();
            }
            // do whatever else
            FinalCheck(current.type);
            return;
        }

        // If user has pressed the Left Mouse Button, do something and
        // swallow it so nothing else hears the event
        if (current.type == EventType.MouseDown && current.button == 0)
        {
            string undoText = "Drawing On " + drawingSurface.name;
            UnityEditor.Undo.RegisterCompleteObjectUndo(drawingSurface, undoText);
            // Gets any active Scriptable Objects we want to be recorded
            foreach (ScriptableObject SO in drawingSurface.activeChannel.ActiveSOs())
            {
                UnityEditor.Undo.RegisterCompleteObjectUndo(SO, undoText);
            }

            UnityEditor.Undo.FlushUndoRecordObjects();

            pointerDown = true;
            drawingSurface.PointerDown(
                HelperFunctions.Vec2ToVec2Int(
                HandleUtility.GUIPointToScreenPixelCoordinate(current.mousePosition)
                )
                );

            //EditorUtility.SetDirty(drawingSurface);
            //Selection.activeGameObject = drawingSurface.gameObject; // dont actually need this
        }

        if (pointerDown == false)
        {
            FinalCheck(current.type);
            return;
        }

        if (current.type == EventType.MouseDrag && current.button == 0)
        {
            drawingSurface.PointerDrag(HelperFunctions.Vec2ToVec2Int(HandleUtility.GUIPointToScreenPixelCoordinate(current.mousePosition)));
        }

        if (current.type == EventType.MouseUp && current.button == 0)
        {
            PointerUpActions();
        }
    }

    private void PointerUpActions()
    {
        pointerDown = false;
        drawingSurface.PointerUp();
        serializedObject.ApplyModifiedProperties();
        drawingSurface.Initialize();
    }

    private void FinalCheck(EventType eventType)
    {
        // After you've done all your custom event interpreting and swallowing,
        // you have to call this code to make sure swallowed events don't bleed out.
        // Not sure why, but that's the rules.
        if (eventType == EventType.Layout)
            HandleUtility.AddDefaultControl(GUIUtility.GetControlID(GetHashCode(), FocusType.Passive));
    }
}
