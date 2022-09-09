using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class DrawingUVWindow : EditorWindow
{
    private bool enableWireframe = true;
    private Vector2 scrollPercent = Vector2.one * .5f;
    private float zoom = 1;
    private bool pointerDown = false;
    private DrawingSurface drawingSurface;

    private static readonly float maxZoom = 5;
    private static readonly float minZoom = .15f;
    private static readonly float zoomInRate = 1.1f;
    private static readonly float zoomOutRate;
    private static readonly float snapThreshold;
    private static readonly float lineHeight;
    private static readonly Color backgroundColor;

    static DrawingUVWindow()
    {
        zoomOutRate = 1f / zoomInRate;
        snapThreshold = (1 * zoomOutRate) * .09f;
        lineHeight = EditorGUIUtility.singleLineHeight;
        backgroundColor = new Color(.15f, .15f, .15f, 1);
    }

    [MenuItem("Window/UV Drawing")]
    public static void OpenWindow()
    {
        GetWindow<DrawingUVWindow>("UV Drawing");
    }

    private void OnEnable()
    {
        UnityEditor.Undo.undoRedoPerformed += UndoCallback;
    }

    private void OnDisable()
    {
        UnityEditor.Undo.undoRedoPerformed -= UndoCallback;

        if (pointerDown)
        {
            PointerUpActions();
        }
    }
    private void OnLostFocus()
    {
        if (pointerDown)
        {
            PointerUpActions();
        }
    }

    private void OnSelectionChange()
    {
        if (pointerDown)
        {
            PointerUpActions();
        }
        Repaint();
    }

    void UndoCallback()
    {
        if(drawingSurface != null)
        {
            drawingSurface.Initialize();
        }
        Repaint();
    }

    private void OnGUI()
    {
        Event current = Event.current;

        GUIStyle centeredBold = new GUIStyle(GUI.skin.label);
        centeredBold.alignment = TextAnchor.MiddleCenter;
        centeredBold.fontStyle = FontStyle.Bold;

        Rect topControlsRect = new Rect(0, 0, position.width, lineHeight * 3);

        Rect availableRect = new Rect(0, topControlsRect.height, position.width, position.height - topControlsRect.height);

        drawingSurface = DrawingSurfaceStream.drawingSurface;
        if (drawingSurface == null)
        {
            EditorGUI.DrawRect(availableRect, backgroundColor);
            EditorGUI.LabelField(
                availableRect,
                "Select a Drawing Surface to Begin Editing",
                centeredBold);
            return;
        }
        DrawingChannel activeChannel = drawingSurface.activeChannel;
        if (activeChannel == null)
        {
            EditorGUI.DrawRect(availableRect, backgroundColor);
            EditorGUI.LabelField(
                availableRect,
                "No active Channels",
                centeredBold);
            return;
        }
        // need better check than just if layers exis
        // well maybe not. I guess its the same as how
        // the 3d objects handle it
        if (activeChannel.layers.Count < 1)
        {
            EditorGUI.DrawRect(availableRect, backgroundColor);
            EditorGUI.LabelField(
                availableRect,
                "No Drawable Layers",
                centeredBold);
            return;
        }

        Vector2 textureResolution = activeChannel.resolution;
        Vector2 textureSize = textureResolution * zoom;

        Rect clipRect = availableRect;

        // draw scroll bars

        Vector2 sizeRatio = availableRect.size / textureSize;
        if (textureSize.x > availableRect.width)
        {
            clipRect.height -= lineHeight;
            scrollPercent.x = GUI.HorizontalScrollbar(
                new Rect(clipRect.x, clipRect.y + clipRect.height, availableRect.width - lineHeight, lineHeight),
                scrollPercent.x,
                sizeRatio.x,
                0,
                1 + sizeRatio.x
                );
        }
        if (textureSize.y > availableRect.height)
        {
            clipRect.width -= lineHeight;
            scrollPercent.y = GUI.VerticalScrollbar(
                new Rect(clipRect.x + clipRect.width, clipRect.y, lineHeight, availableRect.height - lineHeight),
                scrollPercent.y,
                sizeRatio.y,
                0,
                1 + sizeRatio.y
                );
        }

        GUI.BeginClip(clipRect);

        Rect canvasRect = new Rect(0, 0, clipRect.width, clipRect.height);
        EditorGUI.DrawRect(canvasRect, backgroundColor);

        Vector2 minMaxOffsetX = new Vector2(canvasRect.x, (canvasRect.width - textureSize.x));
        Vector2 minMaxOffsetY = new Vector2(canvasRect.y, (canvasRect.height - textureSize.y));
        Rect textureRect = new Rect(
            Mathf.Lerp(minMaxOffsetX.x, minMaxOffsetX.y, scrollPercent.x),
            Mathf.Lerp(minMaxOffsetY.x, minMaxOffsetY.y, scrollPercent.y),
            textureSize.x,
            textureSize.y
            );
        Vector2 workspaceMouse = new Vector2(
            Mathf.Clamp(current.mousePosition.x / canvasRect.width, 0, 1),
            Mathf.Clamp(current.mousePosition.y / canvasRect.height, 0, 1)
            );

        // this clip draws the texture
        GUI.BeginClip(textureRect);

        Vector2 relativeMouse = current.mousePosition / textureRect.size;
        Vector2 uvMouse = relativeMouse;
        uvMouse.y = 1 - uvMouse.y;

        // draws the actual texture being edited
        Rect drawRect = new Rect(0, 0, textureRect.width, textureRect.height);
        Rect checkerRect = new Rect(0, 0, 80 * zoom, 80 * zoom);
        GUI.DrawTextureWithTexCoords(drawRect, EditorStaticMembers.transparentChecker, checkerRect);
        GUI.DrawTexture(drawRect, activeChannel.outputTexture, ScaleMode.StretchToFill);
        GUI.EndClip();

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

                if (prevZoom != zoom)
                {
                    Vector2 newSize = textureResolution * zoom;
                    float scaleSize = zoom / prevZoom;

                    Vector2 goalPos = (workspaceMouse * canvasRect.size) - ((relativeMouse * textureRect.size) * scaleSize);
                    Vector2 predictedOver = (goalPos + newSize) - canvasRect.size;

                    Vector2 newScrollPercent = -goalPos / (-goalPos + predictedOver);
                    scrollPercent = new Vector2(
                        newSize.x > availableRect.width ? newScrollPercent.x : .5f,
                        newSize.y > availableRect.height ? newScrollPercent.y : .5f
                        );
                }
            }
            Repaint();
        }

        if (current.type == EventType.MouseDown && current.button == 0)
        {
            string undoText = "UV Drawing On " + drawingSurface.name;
            UnityEditor.Undo.RegisterCompleteObjectUndo(drawingSurface, undoText);
            foreach (ScriptableObject SO in drawingSurface.activeChannel.ActiveSOs())
            {
                UnityEditor.Undo.RegisterCompleteObjectUndo(SO, undoText);
            }
            UnityEditor.Undo.FlushUndoRecordObjects();

            pointerDown = true;
            drawingSurface.UVPointerDown(uvMouse);
            Repaint();
        }

        if (current.type == EventType.MouseDrag && current.button == 0)
        {
            drawingSurface.UVPointerDrag(uvMouse);
            Repaint();
        }

        if (current.type == EventType.MouseUp && current.button == 0)
        {
            PointerUpActions();
            Repaint();
        }

        if (enableWireframe)
        {
            if (DrawingSurfaceStream.uvLines.Length >= 2)
            {
                Handles.color = new Color(1, 1, 1, zoom >= 1 ? .5f : .5f * zoom);
                Handles.matrix = Matrix4x4.TRS(
                    new Vector3(textureRect.x, textureRect.y),
                    Quaternion.identity,
                    new Vector3(textureRect.width, textureRect.height)
                    );
                Handles.DrawLines(DrawingSurfaceStream.uvLines);
            }
        }

        GUI.EndClip();

        //GUI.BeginGroup();
        //enableWireframe = GUILayout.Toggle(enableWireframe, "Enable Wireframe");

        // this works for now, but wont if the mouse moves outside 
        // the workspace while drawing
        /*
        if (worspaceRect.Contains(current.mousePosition))
        {
            Repaint();
        }
        */
    }

    private void PointerUpActions()
    {
        pointerDown = false;

        if(drawingSurface != null)
        {
            drawingSurface.UVPointerUp();
            // dont know if serialized object part matters here
            //serializedObject.ApplyModifiedProperties();
            drawingSurface.Initialize();
        }
    }
}
