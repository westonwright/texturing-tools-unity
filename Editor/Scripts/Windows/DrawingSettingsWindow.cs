using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System;

//[Serializable]
public class DrawingSettingsWindow : EditorWindow
{
    private string EditorPrefsPath {
        get =>
            PlayerSettings.companyName + "." + 
            PlayerSettings.productName + "." +
            "DrawingSettings";
    }

    [SerializeField]
    private string saveLoadPath = "";
    // Move whatever is currently being used to the front of the list
    [SerializeReference]
    private List<DrawingActor> drawingActors = new List<DrawingActor>();

    [MenuItem("Window/Drawing/Drawing Settings")]
    public static void OpenWindow()
    {
        GetWindow<DrawingSettingsWindow>("Drawing Settings");
    }

    private void OnEnable()
    {
        string data = EditorPrefs.GetString(EditorPrefsPath);
        if (data != "")
        {
            JsonUtility.FromJsonOverwrite(data, this);
        }

        Initialize();
    }

    private void OnDisable()
    {
        string data = JsonUtility.ToJson(this);
        EditorPrefs.SetString(EditorPrefsPath, data);
    }

    private void Initialize()
    {
        if (drawingActors.Count == 0)
        {
            SetOrCreateNewDrawingActor(DrawingActorType.Brush);
        }
        else
        {
            for(int i = 0; i < drawingActors.Count; i++)
            {
                drawingActors[i].EditorTempInitialize();
            }
            DrawingActorStream.UpdateDrawingActor(drawingActors[0]);
        }
    }

    private void SetOrCreateNewDrawingActor(DrawingActorType type)
    {
        int oldIndex = drawingActors.FindIndex(x => x.typeEnum == type);
        DrawingActor tempActor = null;
        string tempString = "";
        if (oldIndex != -1)
        {
            tempActor = drawingActors[oldIndex];
            drawingActors.RemoveAt(oldIndex);
        }
        else
        {
            switch (type)
            {
                case DrawingActorType.Brush:
                    tempActor = new DrawingActorBrush(); 
                    break;
                case DrawingActorType.Eraser:
                    tempActor = new DrawingActorEraser();
                    break;
            }
        }
        drawingActors.Insert(0, tempActor);
        DrawingActorStream.UpdateDrawingActor(drawingActors[0]);
    }

    private void SetNewDrawingActor(DrawingActor drawingActor)
    {
        int oldIndex = drawingActors.FindIndex(x => x.typeEnum == drawingActor.typeEnum);
        if (oldIndex != -1)
        {
            drawingActors.RemoveAt(oldIndex);
        }
        drawingActors.Insert(0, drawingActor);
        DrawingActorStream.UpdateDrawingActor(drawingActors[0]);
    }

    private void OnGUI()
    {
        if(GUILayout.Button("Reset"))
        {
            //Undo.RegisterCompleteObjectUndo(this, )
            SetNewDrawingActor(DrawingActorSaveLoad.LoadDrawingActor(saveLoadPath));
        }
        if (GUILayout.Button("Load Preset"))
        {
            DrawingActor presetActor = DrawingActorSaveLoad.LoadDrawingActor(ref saveLoadPath);
            if(presetActor != null)
            {
                SetNewDrawingActor(presetActor);
            }
            else
            {
                EditorUtility.DisplayDialog(
                    "Load Drawing Preset",
                    "Selected file was not a valid preset!",
                    "ok"
                    );
            }
        }

        if (GUILayout.Button("Save Preset"))
        {
            saveLoadPath = DrawingActorSaveLoad.SaveDrawingActor(drawingActors[0], saveLoadPath) ?? saveLoadPath;
        }
        if (GUILayout.Button("Save Preset as..."))
        {
            saveLoadPath = DrawingActorSaveLoad.SaveDrawingActor(drawingActors[0]) ?? saveLoadPath;
        }

        EditorGUI.BeginDisabledGroup(!(saveLoadPath.Length > 0));
            if (GUILayout.Button("Open in Finder"))
            {
                EditorUtility.RevealInFinder(saveLoadPath);
            }
        EditorGUI.EndDisabledGroup();

        if (drawingActors[0] == null) Initialize();

        EditorGUILayout.BeginHorizontal();
        //GUILayout.ExpandWidth(false);
        EditorGUILayout.LabelField("Type:", GUILayout.MaxWidth(35));
        DrawingActorType tempActorType = drawingActors[0].typeEnum;
        tempActorType = (DrawingActorType)EditorGUILayout.EnumPopup(tempActorType);
        GUILayout.FlexibleSpace();
        EditorGUILayout.EndHorizontal();
        if (tempActorType != drawingActors[0].typeEnum)
        {
            SetOrCreateNewDrawingActor(tempActorType);
        }

        drawingActors[0].DrawGUI(new Vector4(5, 20, position.width - 5, position.height - 5));
    }
}
