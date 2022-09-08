using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public partial class DrawingSurfaceEditor : Editor
{
    Vector2 layerScrollPosition = Vector2.zero;


    List<RenderTexture> layerThumbnails = new List<RenderTexture>();
    // make initialization for layer thumbnails,
    // and functions for adding, removing, and reordering

    public override void OnInspectorGUI()
    {
        // remember to disable drawing mode when the final layer has been deleted

        // maybe figure out how to undo/redo this tickbox
        // there should never be 0 layers anyway
        //if (serializedLayerList == null) return;

        //EditorGUI.BeginDisabledGroup(serializedLayerList.arraySize < 1);
        if (drawingMode)
        {
            if (Tools.current != Tool.None)
            {
                Tools.current = Tool.None;
            }
            GUI.backgroundColor = Color.green;
            //GUI.contentColor = Color.white;
            if (GUILayout.Button("Drawing Mode"))
            {
                DisableDrawMode();
            }
        }
        else
        {
            GUI.backgroundColor = Color.red;
            if (GUILayout.Button("Transform Mode"))
            {
                EnableDrawMode();
            }
        }
        GUI.backgroundColor = Color.white;
        //EditorGUI.EndDisabledGroup();

        // for opening preview use showmodalutility so it cant be docked

        if (GUILayout.Button("Open Drawing Window"))
        {
            EditorWindow.GetWindow<DrawingWindow>().Show();
        }

        EditorGUILayout.LabelField("Channel", EditorStyles.largeLabel);

        EditorGUILayout.LabelField("Layers", EditorStyles.largeLabel);
        // draw a line?

        //layerScrollPosition = EditorGUILayout.BeginScrollView(layerScrollPosition, EditorStyles.helpBox, GUILayout.MaxHeight(192));
        layerScrollPosition = EditorGUILayout.BeginScrollView(
            layerScrollPosition,
            GUI.skin.textArea,
            //GUILayout.MaxHeight(192));
            GUILayout.MaxHeight(192 * 2));

        // draw all the layers
        bool change = false;
        for (int i = 0; i < serializedLayerList.arraySize; i++)
        {
            if (DrawLayerPreview(serializedLayerList, serializedLayerIndex, drawingSurface.activeChannel, i, 0))
            {
                change = true;
                break;
            }
        }


        if (change) drawingSurface.Initialize();
        EditorGUILayout.EndScrollView();

        EditorGUILayout.BeginHorizontal();
        // move this somewhere else eventually
        // automatically scroll to position of newly created layer?

        if (
            //GUILayout.Button(new GUIContent('\u271A'.ToString(), "Add a new layer"), GUILayout.Width(EditorGUIUtility.singleLineHeight))
            GUILayout.Button(new GUIContent("New Texture"))
            )
        {
            CreateLayerOfType(DrawingLayerType.Texture);
        }
        if (
            //GUILayout.Button(new GUIContent('\u271A'.ToString(), "Add a new layer"), GUILayout.Width(EditorGUIUtility.singleLineHeight))
            GUILayout.Button(new GUIContent("New Folder"))
            )
        {
            CreateLayerOfType(DrawingLayerType.Folder);
        }

        //EditorGUIUtility.hasModalWindow;
        EditorGUI.BeginDisabledGroup(serializedLayerList.arraySize == 0);
        if(GUILayout.Button(new GUIContent('\u2716'.ToString(), "Delete Selected Layer"), GUILayout.MaxWidth(EditorGUIUtility.singleLineHeight + 2)))
        {
            serializedObject.ApplyModifiedProperties();

            IDrawingLayerHolder trueLayerHolder = LayerHelperEditor.TrueLayerHolder(
                drawingSurface.activeChannel,
                serializedChannel,
                out SerializedProperty serializedTrueLayerHolder
                );

            if (trueLayerHolder.activeLayer == null)
            {
                // idk what else to do here maybe dont even need to check because its already
                // checked before we could get to this point
                return;
            }

            // did this to avoid memory leaking but seemed to cause issues with undo?
            //serializedObject.ApplyModifiedProperties();
            //trueLayerHolder.activeLayer.BeforeDestroy();

            SerializedProperty serializedTrueLayerIndex = serializedTrueLayerHolder.FindPropertyRelative("_activeLayerIndex");
            SerializedProperty serializedTrueLayerList = serializedTrueLayerHolder.FindPropertyRelative("_layers");
            serializedTrueLayerList.DeleteArrayElementAtIndex(serializedTrueLayerIndex.intValue);

            if(serializedTrueLayerList.arraySize == 0)
            {
                serializedTrueLayerIndex.intValue = -1;
            }
            else if(serializedTrueLayerIndex.intValue == serializedTrueLayerList.arraySize)
            {
                serializedTrueLayerIndex.intValue--;
            }
            serializedObject.ApplyModifiedProperties();
            drawingSurface.Initialize();
        }
        EditorGUI.EndDisabledGroup();
        EditorGUILayout.EndHorizontal();

        if (GUILayout.Button("Save Drawing as..."))
        {
            drawingSurface.SaveTexture(drawingSurface.activeChannel.outputTexture, EditorUtility.SaveFilePanel("Save Drawing", "/Assets", "My Texture", "png"));
        }

        //serializedObject.ApplyModifiedProperties();
    }

    // maybe change this so it can add in place or add to folders?
    private void CreateLayerOfType(DrawingLayerType layerType)
    {
        // Need to teel the drawing channel to basically completely renew the layer
        // that was just created because it is technically a copy i think
        serializedLayerList.InsertArrayElementAtIndex(0);
        //serializedLayerList.arraySize += 1;
        //serializedLayerList.MoveArrayElement(serializedLayerList.arraySize, 0);
        serializedLayerIndex.intValue = 0;
        serializedObject.ApplyModifiedProperties();
        //RenewActiveLayer();
        drawingSurface.activeChannel.CreateNewLayerInPlace(layerType);
        drawingSurface.Initialize();
    }

    // layer preview consists of main rect
    // left and right handle areas
    // thumbnail rect
    // and type, name, and number slots

    // when selected this is defined by: left handle rect, right handle rect,
    // thumbnail rect, information rect, and top, middle, and bottom positions
    // this is 7 vars in total
    // this can be defined by: selectable rect, left handle rect, right handle rect, 
    // thumbnail rect, 
    private bool DrawLayerPreview(SerializedProperty serializedLocalLayerList, SerializedProperty serializedLocalLayerIndex, IDrawingLayerHolder layerHolder, int layerIndex, int indentCount)
    {
        DrawingLayerEditorPreview preview = LayerHelperEditor.PreviewFromLayer(serializedObject, serializedChannel, layerHolder, layerIndex, 0, null);
        if (preview.DrawPreview())
            return true;
        return false;
    }

    private void DrawThumbnail(Rect rect, IDrawingLayer layer)
    {
        switch (layer)
        {
            case DrawingLayerTexture drawingLayerTexture:
                GUI.Box(rect, drawingLayerTexture.outputTexture);

                break;
            case DrawingLayerFolder:
                // either draw folder icon with number for how many components
                // or draw preview of all components combined
                break;
        }

    }
}
