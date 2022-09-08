using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StrokePoint
{
    private Vector2Int _pixelPosition;
    public Vector2Int pixelPosition { get => _pixelPosition; }

    private bool _continuous;
    public bool continuous { get => _continuous; }

    public StrokePoint(Vector2Int pixelPosition, bool continuous)
    {
        _pixelPosition = pixelPosition;
        _continuous = continuous;
    }
}