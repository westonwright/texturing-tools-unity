using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class DrawingUVWindow : EditorWindow
{
    private bool enableWireframe = true;
    private Vector2 scrollPercent = Vector2.one * .5f;
    private float zoom = 1;

    private static readonly float maxZoom = 5;
    private static readonly float minZoom = .25f;
    private static readonly float zoomInRate = 1.1f;
    private static readonly float zoomOutRate;
    private static readonly float snapThreshold;

    static DrawingUVWindow()
    {
        zoomOutRate = 1f / zoomInRate;
        snapThreshold = (1 * zoomOutRate) * .09f;
    }

    [MenuItem("Window/UV Drawing")]
    public static void OpenWindow()
    {
        GetWindow<DrawingUVWindow>("UV Drawing");
    }

    private void OnGUI()
    {
        Event current = Event.current;

        if (DrawingSurfaceStream.drawingSurface == null)
        {
            // put this in the center
            EditorGUILayout.LabelField("Select a Drawing Surface to Begin Editing");
            return;
        }
        if (DrawingSurfaceStream.drawingSurface.activeChannel == null)
        {
            EditorGUILayout.LabelField("No active Channels");
            return;
        }
        if (DrawingSurfaceStream.drawingSurface.activeChannel.layers.Count < 1)
        {
            EditorGUILayout.LabelField("No Drawable Layers");
            return;
        }


        EditorGUILayout.BeginHorizontal();
        enableWireframe = GUILayout.Toggle(enableWireframe, "Enable Wireframe");
        EditorGUILayout.EndHorizontal();
        
        EditorGUILayout.BeginHorizontal();
        //enableWireframe = GUILayout.Toggle(enableWireframe, "Enable Wireframe");
        EditorGUILayout.EndHorizontal();

        Rect canvasRect = GUILayoutUtility.GetRect(position.width, position.height - (EditorGUIUtility.singleLineHeight * 2));

        Vector2 textureResolution = DrawingSurfaceStream.drawingSurface.activeChannel.resolution;
        Vector2 textureSize = textureResolution * zoom;
        Vector2 minMaxOffsetX = new Vector2(canvasRect.x, -(textureSize.x - canvasRect.x));
        Vector2 minMaxOffsetY = new Vector2(canvasRect.y, -(textureSize.y - canvasRect.y));
        Rect textureRect = new Rect(
            Mathf.Lerp(minMaxOffsetX.x, minMaxOffsetX.y, scrollPercent.x),
            Mathf.Lerp(minMaxOffsetY.x, minMaxOffsetY.y, scrollPercent.y),
            textureSize.x,
            textureSize.y
            );
        EditorGUI.DrawRect(canvasRect, Color.gray);

        Vector2 portionSeenX = new Vector2(
            Mathf.Max(-textureRect.x, canvasRect.min.x),
            Mathf.Min(textureRect.max.x, canvasRect.max.x)
            );
        Vector2 seenPercentX = new Vector2(
            portionSeenX.x / textureRect.width,
            (portionSeenX.y / textureRect.width) + (portionSeenX.x / textureRect.width)
            );
        Vector2 portionSeenY = new Vector2(
            Mathf.Max(-textureRect.y, canvasRect.min.y),
            Mathf.Min(textureRect.max.y, canvasRect.max.y)
            );
        Vector2 seenPercentY = new Vector2(
            portionSeenY.x / textureRect.height,
            (portionSeenY.y / textureRect.height) + (portionSeenY.x / textureRect.height)
            );
        Vector2 workspaceMouse = new Vector2(
            Mathf.Clamp(current.mousePosition.x / canvasRect.width, 0, 1),
            Mathf.Clamp(current.mousePosition.y / canvasRect.height, 0, 1)
            );
        Vector2 relativeMouse = new Vector2(
            Mathf.Lerp(seenPercentX.x, seenPercentX.y, workspaceMouse.x),
            Mathf.Lerp(seenPercentY.x, seenPercentY.y, workspaceMouse.y)
            );

        if (current.type == EventType.ScrollWheel)
        {
            if (Mathf.Abs(current.delta.y) >= 1)
            {
                float prevZoom = zoom;
                // positive is zooming out
                if (current.delta.y >= 0)
                    zoom *= zoomOutRate;
                else
                    zoom *= zoomInRate;

                // snap near 100
                if (Mathf.Abs(zoom - 1f) < snapThreshold)
                {
                    zoom = 1f;
                }
                zoom = Mathf.Clamp(zoom, minZoom, maxZoom);
                if(zoom > 1)
                {
                    if (prevZoom != zoom)
                    {
                        // y = (relativeMouse - workspaceMouse) * newSize

                        Vector2 newSize = textureResolution * zoom;
                        float scaleSize = zoom / prevZoom;

                        Vector2 goalPos = (workspaceMouse * canvasRect.size) - ((relativeMouse * textureRect.size) * scaleSize);
                        //Vector2 goalPos = textureRect.position + (relativeMouse - workspaceMouse) * newSize;
                        Vector2 predictedOver = (goalPos + newSize) - canvasRect.size;

                        scrollPercent = -goalPos / (-goalPos + predictedOver);
                    }
                }
                // if its too small, just keep it in the center
                else
                {
                    scrollPercent = Vector2.one * .5f;
                }
            }
            Repaint();
        }

        if (enableWireframe)
        {
            if (DrawingSurfaceStream.uvLines.Length >= 2)
            {
                Handles.matrix = Matrix4x4.TRS(
                    new Vector3(textureRect.x, textureRect.y),
                    Quaternion.identity,
                    new Vector3(textureRect.width, textureRect.height)
                    );
                Handles.DrawLines(DrawingSurfaceStream.uvLines);
            }
        }

        // this works for now, but wont if the mouse moves outside 
        // the workspace while drawing
        /*
        if (worspaceRect.Contains(current.mousePosition))
        {
            Repaint();
        }
        */
    }
}
