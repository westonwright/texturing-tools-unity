using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(DrawingSurface))]
public partial class DrawingSurfaceEditor : Editor
{
    DrawingSurface drawingSurface;
    bool pointerDown = false;

    SerializedProperty serializedChannelIndex;
    SerializedProperty serializedChannelList;
    SerializedProperty serializedChannel;
    SerializedProperty serializedLayerIndex;
    SerializedProperty serializedLayerList;

    Tool lastTool = Tool.None;

    bool drawingMode = false;


    private void OnEnable()
    {
        UnityEditor.Undo.undoRedoPerformed += UndoCallback;

        drawingSurface = (DrawingSurface)target;
        drawingSurface.GetRequiredComponents();
        drawingSurface.Initialize();

        serializedChannelIndex = serializedObject.FindProperty("activeChannelIndex");
        serializedChannelList = serializedObject.FindProperty("drawingChannels");
        serializedChannel = serializedChannelList.GetArrayElementAtIndex(serializedChannelIndex.intValue);
        serializedLayerList = serializedChannel.FindPropertyRelative("_layers");
        serializedLayerIndex = serializedChannel.FindPropertyRelative("_activeLayerIndex");
        DrawingSurfaceStream.SetDrawingSurface(drawingSurface);
    }

    private void OnDisable()
    {
        UnityEditor.Undo.undoRedoPerformed -= UndoCallback;

        drawingSurface.PointerUp();

        if(lastTool != Tool.None)
            Tools.current = lastTool;
        DrawingSurfaceStream.SetDrawingSurface(null);
    }

    // must re-initialize when an undo is performed
    void UndoCallback()
    {
        drawingSurface.Initialize();
    }

    private void EnableDrawMode()
    {
        lastTool = Tools.current;
        Tools.current = Tool.None;
        pointerDown = false;
        drawingMode = true;
    }

    private void DisableDrawMode()
    {
        Tools.current = lastTool;
        if (pointerDown)
        {
            PointerUpActions(); // not sure if this is need here but just in case;
        }
        drawingMode = false;
    }

    private void RenewActiveChannel()
    {
        serializedChannel = serializedChannelList.GetArrayElementAtIndex(serializedChannelIndex.intValue);
        
        serializedLayerList = serializedChannel.FindPropertyRelative("_layers");
        serializedLayerIndex = serializedChannel.FindPropertyRelative("_activeLayerIndex");
    }
}
