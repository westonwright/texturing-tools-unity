using System.Collections;
using System.Collections.Generic;
using UnityEngine;
public class DrawingActorJSONSelector
{
    [SerializeField]
    DrawingActorType drawingActorType;

    public DrawingActor DrawingActor(string json)
    {
        switch (drawingActorType)
        {
            case DrawingActorType.Brush:
                return JsonUtility.FromJson<DrawingActorBrush>(json);
            case DrawingActorType.Eraser:
                return JsonUtility.FromJson<DrawingActorEraser>(json);
        }
        return new DrawingActorBrush();
    }
}