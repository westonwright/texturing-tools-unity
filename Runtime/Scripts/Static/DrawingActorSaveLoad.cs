using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;

public static class DrawingActorSaveLoad
{
    /// <summary>
    /// Loads a Drawing Actor and saves the path chosen
    /// </summary>
    /// <param name="path"></param>
    /// <returns></returns>
    public static DrawingActor LoadDrawingActor(ref string path)
    {
        string newPath = EditorUtility.OpenFilePanel("Load Drawing Actor", "/Assets", "json");
        if(newPath.Length > 0)
        {
            path = newPath;
            return LoadDrawingActor(path);
        }
        else
        {
            return null;
        }
    }

    /// <summary>
    /// Loads a Drawing Actor from the provided path
    /// </summary>
    /// <param name="path"></param>
    /// <returns></returns>
    public static DrawingActor LoadDrawingActor(string path)
    {
        if (path.Length > 0 && System.IO.File.Exists(path))
        {
            string json = System.IO.File.ReadAllText(path);

            DrawingActorJSONSelector selector = JsonUtility.FromJson<DrawingActorJSONSelector>(json);
            DrawingActor drawingActor = selector.DrawingActor(json);
            drawingActor.EditorInitialize();
            return drawingActor;
        }
        // might want way to reset based on type of actor being used if not based on a preset
        // currently it will just always go back to the default brush settings
        return new DrawingActorBrush();
    }

    public static string SaveDrawingActor(DrawingActor drawingActor, string path)
    {
        if (path.Length > 0)
        {
            string json = GetActorJson(drawingActor);
            System.IO.File.WriteAllText(path, json);
            return path;
        }
        else
        {
            return SaveDrawingActor(drawingActor);
        }
    }

    public static string SaveDrawingActor(DrawingActor drawingActor)
    {
        string json = GetActorJson(drawingActor);
        string path = EditorUtility.SaveFilePanel("Save Drawing Actor", "/Assets", drawingActor.typeEnum.ToString(), "json");
        if (path.Length > 0)
        {
            System.IO.File.WriteAllText(path, json);
            return path;
        }
        else
        {
            return null;
        }
    }

    private static string GetActorJson(DrawingActor drawingActor)
    {
        drawingActor.EditorSave();
        return JsonUtility.ToJson(drawingActor);
    }
}
#endif