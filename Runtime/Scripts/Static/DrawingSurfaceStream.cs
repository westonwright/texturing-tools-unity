using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class DrawingSurfaceStream
{
    private static DrawingSurface _drawingSurface;
    public static DrawingSurface drawingSurface { get => _drawingSurface; } // this can be null

    private static Vector3[] _uvLines;
    public static Vector3[] uvLines
    {
        get
        {
            if( _uvLines == null)
            {
                _uvLines = new Vector3[0];
            }
            return _uvLines;
        }
    }

    public static void SetDrawingSurface(DrawingSurface newSurface)
    {
        _drawingSurface = newSurface;
        CreateUVArray();
    }

    private static void CreateUVArray()
    {
        if(drawingSurface == null)
        {
            _uvLines = new Vector3[0];
            return;
        }

        List<Vector3> points = new List<Vector3>();

        List<Vector2> uvs = new List<Vector2>();
        drawingSurface.surfaceMesh.GetUVs(0, uvs);
        int[] t = drawingSurface.surfaceMesh.triangles;
        int stride = t.Length / 3;
        for (int i = 0; i < stride; i += 1)
        {
            points.Add(uvs[t[i * 3]]);
            points.Add(uvs[t[(i * 3) + 1]]);
            points.Add(uvs[t[(i * 3) + 1]]);
            points.Add(uvs[t[(i * 3) + 2]]);
            points.Add(uvs[t[(i * 3) + 2]]);
            points.Add(uvs[t[i * 3]]);
        }

        _uvLines = points.ToArray();
    }
}
