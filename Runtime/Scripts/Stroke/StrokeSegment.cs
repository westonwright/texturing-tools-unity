using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StrokeSegment
{
    private Vector2Int _pixelPosition;
    public Vector2Int pixelPosition { get => _pixelPosition; }

    // the pixel position to perform the operation
    private Vector2Int _prevPixelPosition;
    public Vector2Int prevPixelPosition { get => _prevPixelPosition; }
    
    // the motion of the stroke from its last drawn point
    // make motion vector a vector2Int?
    private Vector2Int _motionVector;
    public Vector2Int motionVector { get => _motionVector; }

    public StrokeSegment(Vector2Int pixelPosition)
    {
        _pixelPosition = pixelPosition;
        _prevPixelPosition = pixelPosition;
        _motionVector = Vector2Int.zero;
    }

    public StrokeSegment(Vector2Int pixelPosition, StrokeSegment prevSegment)
    {
        _pixelPosition = pixelPosition;
        _prevPixelPosition = prevSegment.pixelPosition;
        _motionVector = _pixelPosition - _prevPixelPosition;
    }

}
