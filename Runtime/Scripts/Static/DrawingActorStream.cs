using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class DrawingActorStream
{
    private static DrawingActor _drawingActor;
    public static DrawingActor drawingActor 
    { get
        {
            if(_drawingActor == null)
            {
                _drawingActor = new DrawingActorBrush();
            }
            return _drawingActor;
        }
    }

    /*
    private static bool _drawingEnabled;
    public static bool drawingEnabled { get => _drawingEnabled; set => _drawingEnabled = value; }

    static DrawingActorStream()
    {
        _drawingEnabled = false;
    }
    */
    public static void UpdateDrawingActor(DrawingActor newActor)
    {
        _drawingActor = newActor;
    }
}
