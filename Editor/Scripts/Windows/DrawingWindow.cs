using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class DrawingWindow : EditorWindow
{
    private string saveLoadPath = "";
    private DrawingActorType drawingActorType = DrawingActorType.brush;
    private Dictionary<DrawingActorType, DrawingActor> drawingActors = new Dictionary<DrawingActorType, DrawingActor>();
    private DrawingActor drawingActor;

    [MenuItem("Window/Drawing")]
    public static void OpenWindow()
    {
        GetWindow<DrawingWindow>("Drawing");
    }

    private void Initialize()
    {
        SetNewDrawingActor(new DrawingActorBrush());
        drawingActorType = DrawingActorType.brush;
        saveLoadPath = "";
    }

    private void SetNewDrawingActor(DrawingActor newActor)
    {
        drawingActor = newActor;
        DrawingActorStream.UpdateDrawingActor(drawingActor);
    }

    private void Awake()
    {
        Initialize();
    }

    private void OnGUI()
    {
        if(GUILayout.Button("Reset"))
        {
            SetNewDrawingActor(DrawingActorSaveLoad.LoadDrawingActor(saveLoadPath));
            drawingActorType = drawingActor.actorEnum;
        }
        if (GUILayout.Button("Load Preset"))
        {
            SetNewDrawingActor(DrawingActorSaveLoad.LoadDrawingActor(ref saveLoadPath) ?? drawingActor);
            drawingActorType = drawingActor.actorEnum;
        }

        if (GUILayout.Button("Save Preset"))
        {
            saveLoadPath = DrawingActorSaveLoad.SaveDrawingActor(drawingActor, saveLoadPath) ?? saveLoadPath;
        }
        if (GUILayout.Button("Save Preset as..."))
        {
            saveLoadPath = DrawingActorSaveLoad.SaveDrawingActor(drawingActor) ?? saveLoadPath;
        }

        EditorGUI.BeginDisabledGroup(!(saveLoadPath.Length > 0));
            if (GUILayout.Button("Open in Finder"))
            {
                EditorUtility.RevealInFinder(saveLoadPath);
            }
        EditorGUI.EndDisabledGroup();

        if (drawingActor == null) Initialize();


        EditorGUILayout.BeginHorizontal();
        //GUILayout.ExpandWidth(false);
        EditorGUILayout.LabelField("Type:", GUILayout.MaxWidth(35));
        DrawingActorType tempActorType = (DrawingActorType)EditorGUILayout.EnumPopup(drawingActorType);
        GUILayout.FlexibleSpace();
        EditorGUILayout.EndHorizontal();
        if (tempActorType != drawingActorType)
        {
            if (drawingActors.TryGetValue(tempActorType, out DrawingActor savedActor))
            {
                SetNewDrawingActor(savedActor);
            }
            else
            {
                DrawingActor newActor = null;
                switch (tempActorType)
                {
                    case DrawingActorType.brush:
                        newActor = new DrawingActorBrush();
                        break;
                    case DrawingActorType.pencil:
                        // new pencil
                        break;
                    case DrawingActorType.eraser:
                        // new eraser
                        break;
                }
                drawingActors.Add(tempActorType, newActor);
            }

            drawingActorType = tempActorType;
        }

        drawingActor.DrawGUI(new Vector4(5, 20, position.width - 5, position.height - 5));
    }
}
