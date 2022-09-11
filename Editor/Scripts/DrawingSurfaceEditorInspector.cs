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
        GUIStyle boldButton = new GUIStyle(GUI.skin.button);
        boldButton.fontStyle = FontStyle.Bold;
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

        if (GUILayout.Button("Open Drawing Settings"))
        {
            DrawingSettingsWindow.OpenWindow();
        }
        if (GUILayout.Button("Open UV Drawing Panel"))
        {
            DrawingUVWindow.OpenWindow();
        }

        EditorGUILayout.LabelField("Channel", EditorStyles.largeLabel);

        // texture color mode
        //SerializedProperty serializedTextureFormat = serializedChannel.FindPropertyRelative("_textureFormat");
        //serializedTextureFormat.enumValueIndex = EditorGUILayout.Popup(serializedTextureFormat.enumValueIndex, serializedTextureFormat.enumDisplayNames);

        EditorGUILayout.BeginHorizontal();

        string[] channelOptions = new string[serializedChannelList.arraySize];
        for (int i = 0; i < channelOptions.Length; i++)
        {
            channelOptions[i] = drawingSurface.channels[i].name;
        }

        int newChannelIndex = EditorGUILayout.Popup(serializedChannelIndex.intValue, channelOptions);
        if(newChannelIndex != serializedChannelIndex.intValue)
        {
            serializedChannelIndex.intValue = newChannelIndex;
            RenewActiveChannel();
            serializedObject.ApplyModifiedProperties();
            drawingSurface.Initialize();
        }

        if(GUILayout.Button(new GUIContent("+", "Add a New Channel"), boldButton))
        {
            serializedChannelList.InsertArrayElementAtIndex(0);
            serializedChannelIndex.intValue = 0;
            serializedObject.ApplyModifiedProperties();
            drawingSurface.CreateNewChannelInPlace();
            // clear out copied layer refrences
            SerializedProperty serializedLayers = serializedChannelList.GetArrayElementAtIndex(0).FindPropertyRelative("_layers");
            int size = serializedLayers.arraySize;
            while(size > 0)
            {
                serializedLayers.DeleteArrayElementAtIndex(size - 1);
                size--;
            }
            serializedObject.ApplyModifiedProperties();
            drawingSurface.Initialize();
            RenewActiveChannel();
            //serializedObject.ApplyModifiedProperties();
            serializedChannel.FindPropertyRelative("_name").stringValue = EditorStaticMembers.CreateUniqueChannelName(
                serializedChannelList,
                serializedChannelIndex.intValue,
                "_MainTex"
                );

            //serializedObject.ApplyModifiedProperties();
            // add a new layer and switch to it
        }
        EditorGUI.BeginDisabledGroup(serializedChannelList.arraySize <= 1);
        if(GUILayout.Button(new GUIContent("-", "Remove Channel"), boldButton))
        {
            if(EditorUtility.DisplayDialog(
                "Drawing System",
                "Delete the Current Channel \"" + serializedChannel.FindPropertyRelative("_name").stringValue + "\"",
                "Confirm",
                "Cancel"
                )
                )
            {
                // remove current layer
                serializedObject.ApplyModifiedProperties();
                drawingSurface.RemovedChannel(serializedChannel.FindPropertyRelative("_name").stringValue);
                // not doing BeforeDestroy here because it seems to interfere with undos
                serializedChannelList.DeleteArrayElementAtIndex(serializedChannelIndex.intValue);
                serializedChannelIndex.intValue = 0;
                serializedObject.ApplyModifiedProperties();
                drawingSurface.Initialize();
                RenewActiveChannel();
            }
        }
        EditorGUI.EndDisabledGroup();

        SerializedProperty resolutionProperty = serializedChannel.FindPropertyRelative("_resolution");
        GUILayout.FlexibleSpace();
        GUILayout.Label(resolutionProperty.vector2IntValue.x + "x" + resolutionProperty.vector2IntValue.y);
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        SerializedProperty channelName = serializedChannel.FindPropertyRelative("_name");
        string tempString = channelName.stringValue;
        tempString = EditorGUILayout.DelayedTextField(tempString);
        if(tempString != channelName.stringValue)
        {
            tempString = EditorStaticMembers.CreateUniqueChannelName(
            serializedChannelList,
            serializedChannelIndex.intValue,
            tempString
            );

            serializedObject.ApplyModifiedProperties();
            drawingSurface.RemovedChannel(channelName.stringValue);
            channelName.stringValue = tempString;
            serializedObject.ApplyModifiedProperties();
            drawingSurface.Initialize();
        }
        serializedObject.ApplyModifiedProperties();
        EditorGUILayout.EndHorizontal();


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
