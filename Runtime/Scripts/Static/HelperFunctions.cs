using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class HelperFunctions
{
    public static int xThreads(float width)
    {
        return Mathf.CeilToInt(width / DrawingStaticMembers.threadGroupSize);
    }
    public static int yThreads(float height)
    {
        return Mathf.CeilToInt(height / DrawingStaticMembers.threadGroupSize);
    }
    
    // TODO: finish these lerp functions
    public static float SmoothLerp(float min, float max, float t)
    {
        //t = Mathf.Clamp((t - min) / (t - max), 0, 1);

        //return t * t * t * (t * (t * 6 - 15) + 10);
        return t * t * (3 - 2 * t);
    }

    public static float ExpLerp(float min, float max, float strength, float t)
    {
        return Mathf.Pow(t, 2 * strength);
    }

    public static float RemapValue(float inValue, float inMin, float inMax, float outMin, float outMax)
    {
        return (((inValue - inMin) / (inMax - inMin)) * (outMax - outMin)) + outMin;
    }

    public static Vector2Int Vec2ToVec2Int(Vector2 vector2)
    {
        return new Vector2Int((int)vector2.x, (int)vector2.y);
    }
    
    public static Vector2 Vec2IntToVec2(Vector2Int vector2Int)
    {
        return vector2Int;
    }

    public static Vector2 Num2Vec2(float x, float y)
    {
        return new Vector2(x, y);
    }

    public static Vector2 Num2Vec2(int x, int y)
    {
        return new Vector2(x, y);
    }

    public static Vector2Int Num2Vec2Int(float x, float y)
    {
        return new Vector2Int((int)x, (int)y);
    }

    public static Vector2Int Num2Vec2Int(int x, int y)
    {
        return new Vector2Int(x, y);
    }
}
