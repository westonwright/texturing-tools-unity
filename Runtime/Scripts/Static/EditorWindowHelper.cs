using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;

public static class EditorWindowHelper
{
    public static void DrawSlider(ref float val, string name, float min, float max)
    {
        EditorGUILayout.BeginVertical();
        EditorGUILayout.LabelField(name + ":", GUILayout.Width(120));
        val = EditorGUILayout.Slider(val, min, max, GUILayout.MaxWidth(200));
        EditorGUILayout.EndVertical();
    }
    public static void DrawSlider(ref int val, string name, int min, int max)
    {
        EditorGUILayout.BeginVertical();
        EditorGUILayout.LabelField(name + ":", GUILayout.Width(120));
        val = EditorGUILayout.IntSlider(val, min, max, GUILayout.MaxWidth(200));
        EditorGUILayout.EndVertical();
    }
    public static RenderTexture DrawStrokePreview(Vector2Int resolution, DrawingActor drawingActor)
    {
        return TextureCalculations.CreateEmptyTexture(resolution);
        /*
        DrawingChannel drawingChannel = new DrawingChannel(resolution);
        // dont like this. Maybe seperate editor versions from generic version of the drawing components?

        Vector2Int texSize = HelperFunctions.Num2Vec2Int(resolution.x, resolution.y);
        Vector2Int sinRange = HelperFunctions.Num2Vec2Int(Mathf.FloorToInt(texSize.x * .05f), Mathf.CeilToInt(texSize.x * .95f));
        Vector2Int sinSize = HelperFunctions.Num2Vec2Int(sinRange.y - sinRange.x, (texSize.y * .7f) / 2);

        Vector2Int curPointPos = Vector2Int.zero;

        curPointPos = HelperFunctions.Num2Vec2Int(sinRange.x, (Mathf.Sin((sinRange.x - sinRange.x) / (sinSize.x / (Mathf.PI * 2))) * sinSize.y) + (texSize.y * .5f));

        if (drawingActor.spacingEnabled)
        {
            //initial point
            Vector2 pointDirection = new Vector2(.5f, .5f).normalized;
            Vector2 prevPointPos = curPointPos;
            Vector2 prevDrawnPixPos = curPointPos;
            float curPointPixDist = 0;

            for (int i = sinRange.x; i < sinRange.y; i++)
            {
                curPointPos = HelperFunctions.Num2Vec2Int(i, (Mathf.Sin((i - sinRange.x) / (sinSize.x / (Mathf.PI * 2))) * sinSize.y) + (texSize.y * .5f));
                curPointPixDist += Vector2.Distance(curPointPos, prevPointPos);
                while (curPointPixDist >= (drawingActor.spacing * drawingActor.size))
                {
                    pointDirection = curPointPos - prevDrawnPixPos;
                    if (pointDirection.magnitude < (drawingActor.spacing * drawingActor.size))
                    {
                        prevDrawnPixPos = curPointPos;
                    }
                    else
                    {
                        prevDrawnPixPos = prevDrawnPixPos + pointDirection.normalized * (drawingActor.spacing * drawingActor.size);
                    }
                    curPointPixDist -= (drawingActor.spacing * drawingActor.size);
                    drawingChannel.SetStrokeContinuous(curPointPos);
                }
                prevPointPos = curPointPos;
            }
        }
        else
        {
            float progress = 0;
            float xPos = 0;
            for (int i = 0; i < 35; i++)
            {
                //progress = HelperFunctions.SmoothLerp(0, 1, i / 34f);
                progress = HelperFunctions.ExpLerp(0, 1, 1.5f, i / 34f);
                xPos = (sinRange.x + (sinSize.x * progress));
                curPointPos = HelperFunctions.Num2Vec2Int(xPos, (Mathf.Sin(xPos / (sinSize.x / (Mathf.PI * 2))) * sinSize.y) + (texSize.y * .5f));
                drawingChannel.SetStrokeContinuous(curPointPos);
            }
        }
        drawingChannel.FinishStroke();
        RenderTexture finalOutput = TextureCalculations.DuplicateTexture(drawingChannel.outputTexture);
        drawingChannel.ReleaseAll();
        return finalOutput;
        */
    }
}
#endif 
