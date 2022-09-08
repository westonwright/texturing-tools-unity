using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public abstract class DrawingLayerEditorPreview
{
    protected static readonly float layerHeight = 64;
    protected static readonly float indentWidth = 20;

    private float _lineHeight;
    protected float lineHeight { get => _lineHeight; }

    private Rect _totalRect;
    protected Rect totalRect { get => _totalRect; }

    private Rect _leftHandleRect;
    protected Rect leftHandleRect { get => _leftHandleRect; }
    
    private Rect _additionLeftHandleRect;
    protected Rect additionLeftHandleRect { get => _additionLeftHandleRect; }

    private Rect _rightHandleRect;
    protected Rect rightHandleRect { get => _rightHandleRect; }

    private Rect _selectableRect;
    protected Rect selectableRect { get => _selectableRect; }

    private Rect _thumbnailRect;
    protected Rect thumbnailRect { get => _thumbnailRect; }

    private Rect _informationAreaRect;
    protected Rect informationAreaRect { get => _informationAreaRect; }

    private int _layerIndex;
    protected int layerIndex { get => _layerIndex; }

    private int _indentLevel;
    protected int indentLevel { get => _indentLevel; }

    private SerializedObject _serializedObject;
    protected SerializedObject serializedObject { get => _serializedObject; }

    private DrawingLayerEditorPreview _parentEditorPreview;
    protected DrawingLayerEditorPreview parentEditorPreview { get => _parentEditorPreview; }

    private SerializedProperty _serializedLayerHolder;
    protected SerializedProperty serializedLayerHolder { get => _serializedLayerHolder; }

    private IDrawingLayerHolder _layerHolder;
    protected IDrawingLayerHolder layerHolder { get => _layerHolder; }


    /// <summary>
    /// Creates a new preview by reserving a rect and
    /// constructing the necessary parts
    /// </summary>
    public DrawingLayerEditorPreview(SerializedObject serializedObject, SerializedProperty serializedLayerHolder, IDrawingLayerHolder layerHolder, int layerIndex, int indentLevel, DrawingLayerEditorPreview parent = null)
    {
        _parentEditorPreview = parent;
        _serializedObject = serializedObject;
        _serializedLayerHolder = serializedLayerHolder;
        _layerHolder = layerHolder;
        _lineHeight = EditorGUIUtility.singleLineHeight;
        _layerIndex = layerIndex;
        _indentLevel = indentLevel;

        float totalIndent = _indentLevel * indentWidth;
        //float toggleHeight = lineHeight - 4;
        GetControlVals(out float xPos, out float yPos, out float width);
        xPos -= 2;
        yPos -= 1;
        width += 4;

        GUILayoutUtility.GetRect(width, (layerHeight - _lineHeight) - 4);

        _totalRect = new Rect(
            xPos + totalIndent,
            yPos,
            width - totalIndent,
            layerHeight
            );
        _leftHandleRect = new Rect(
            _totalRect.x,
            _totalRect.y, 
            indentWidth, 
            _totalRect.height
            );
        _additionLeftHandleRect = new Rect(
            _totalRect.x + _leftHandleRect.width,
            _totalRect.y, 
            indentWidth, 
            _totalRect.height
            );
        _rightHandleRect = new Rect(
            (_totalRect.x + _totalRect.width) - indentWidth,
            _totalRect.y, 
            indentWidth,
            _totalRect.height
            );
        _selectableRect = new Rect(
            _totalRect.x + (_leftHandleRect.width + _additionLeftHandleRect.width), 
            _totalRect.y,
            _totalRect.width - ((_leftHandleRect.width + additionLeftHandleRect.width) + _rightHandleRect.width),
            _totalRect.height
            );
        _thumbnailRect = new Rect(
            _selectableRect.x,
            _totalRect.y,
            _totalRect.height,
            _totalRect.height
            );
        _informationAreaRect = new Rect(
            _selectableRect.x + _thumbnailRect.width,
            _totalRect.y,
            _selectableRect.width / 2,
            _totalRect.height
            );
    }

    /// <summary>
    /// returns true if any changes were made and surface
    /// needs to be re-initialized
    /// </summary>
    /// <returns></returns>
    public virtual bool DrawPreview()
    {
        SerializedProperty serializedLayerList = serializedLayerHolder.FindPropertyRelative("_layers");
        SerializedProperty serializedLayerIndex = serializedLayerHolder.FindPropertyRelative("_activeLayerIndex");
        SerializedProperty serializedLayer = serializedLayerList.GetArrayElementAtIndex(layerIndex);
        IDrawingLayer layer = layerHolder.layers[layerIndex];

        // if this is the active layer or not
        // need to edit because right now there can be multiple
        // active layers at different depths
        if(layerIndex != serializedLayerIndex.intValue)
        {
            // return false here because if the layer has been selected
            // we can skip everything else, but its false because 
            // we dont need to re-initialize anything
            if (DrawSelectableArea())
                return false;
            DrawUnfocusedBackground();
            DrawNameText(layer);
        }
        // if we are the parent of a currently selected child
        else if (layer is IDrawingLayerHolder && (layer as IDrawingLayerHolder).activeLayerIndex > -1)
        {
            if (DrawSelectableArea())
                return false;
            DrawFocusedBackground();
            DrawNameText(layer);
        }
        else
        {
            DrawFocusedBackground();

            DrawNameField(serializedLayer);
            if (DrawMovementArrows(serializedLayerList, serializedLayerIndex))
                return true;
            if (DrawFolderMovementArrows(serializedLayerList, serializedLayerIndex))
                return true;
        }

        DrawStandardInformation(layer);
        if (DrawVisibilityToggle(serializedLayer))
            return true;
        DrawLockedToggle(serializedLayer);

        //serializedObject.ApplyModifiedProperties();

        return false;
    }

    // if this layer was selected
    protected bool DrawSelectableArea()
    {
        EditorGUIUtility.AddCursorRect(selectableRect, MouseCursor.Link);
        if (GUI.Button(selectableRect, "", GUI.skin.label))
        {
            // dont need to apply properties because the method does that automatically
            SendLayerIndexResetToParent();

            SendLayerIndexUpward();

            serializedObject.ApplyModifiedProperties();
            // unfocuses text field
            EditorGUI.FocusTextInControl("");
            return true;
        }
        return false;
    }

    protected void DrawUnfocusedBackground()
    {
        GUI.Box(selectableRect, "", GUI.skin.textField);
        GUI.Box(leftHandleRect, "", EditorStyles.helpBox);
        // chould allow changing color of the rect for extra identification or something
        GUI.Box(additionLeftHandleRect, "", EditorStyles.helpBox);
    }
    protected void DrawFocusedBackground()
    {
        GUI.Box(selectableRect, "", GUI.skin.window);
        GUI.Box(leftHandleRect, "", EditorStyles.helpBox);
        GUI.Box(additionLeftHandleRect, "", EditorStyles.helpBox);
    }

    protected void DrawStandardInformation(IDrawingLayer layer)
    {
        Rect typeRect = GetSlotRect(informationAreaRect, 0, lineHeight, .15f);
        Rect indexRect = GetSlotRect(informationAreaRect, 0, lineHeight, .85f);

        EditorGUI.BeginDisabledGroup(true);
        GUI.Label(typeRect, layer.layerType.ToString());
        GUI.Label(indexRect, (layerIndex + 1).ToString());
        EditorGUI.EndDisabledGroup();
    }

    protected void DrawNameText(IDrawingLayer layer)
    {
        Rect nameRect = GetSlotRect(informationAreaRect, 0, lineHeight, .5f);
        EditorGUI.LabelField(nameRect, layer.name);
    }

    protected void DrawNameField(SerializedProperty serializedLayer)
    {
        SerializedProperty nameProperty = serializedLayer.FindPropertyRelative("_name");
        Rect nameRect = GetSlotRect(informationAreaRect, 0, lineHeight, .5f);
        nameProperty.stringValue = EditorGUI.TextField(nameRect, nameProperty.stringValue);
        serializedObject.ApplyModifiedProperties();
    }

    // if it switched, needs to be re-initialized
    protected bool DrawVisibilityToggle(SerializedProperty serializedLayer)
    {
        Rect toggleRect = GetSlotRect(leftHandleRect, 6, lineHeight + 2, .5f);
        SerializedProperty visibleProperty = serializedLayer.FindPropertyRelative("_visible");
        bool statingValue = visibleProperty.boolValue;
        statingValue = GUI.Toggle(toggleRect, statingValue, new GUIContent("", "Enable"));
        if(statingValue != visibleProperty.boolValue)
        {
            visibleProperty.boolValue = statingValue;
            serializedObject.ApplyModifiedProperties();
            return true;
        }
        return false;
    }

    // doesnt do anything right now
    protected void DrawLockedToggle(SerializedProperty serializedLayer)
    {
        Rect toggleRect = GetSlotRect(rightHandleRect, 6, lineHeight + 2, .5f);
        SerializedProperty visibleProperty = serializedLayer.FindPropertyRelative("_locked");
        visibleProperty.boolValue = GUI.Toggle(toggleRect, visibleProperty.boolValue, new GUIContent("", "Lock"));
        serializedObject.ApplyModifiedProperties();
    }

    protected bool DrawMovementArrows(SerializedProperty serializedLayerList, SerializedProperty serializedLayerIndex)
    {
        Rect upRect = GetSlotRect(leftHandleRect, 6, lineHeight, .2f);
        Rect downRect = GetSlotRect(leftHandleRect, 6, lineHeight, .8f);

        if (layerIndex != 0)
        {
            if (GUI.Button(upRect, new GUIContent("", "Move Layer Up"), GUI.skin.verticalScrollbarUpButton))
            {
                serializedLayerList.MoveArrayElement(layerIndex, layerIndex - 1);
                serializedLayerIndex.intValue--;
                //RenewActiveLayer();
                //changeMade = true;
                serializedObject.ApplyModifiedProperties();
                return true;
            }

        }
        if (layerIndex < serializedLayerList.arraySize - 1)
        {
            if (GUI.Button(downRect, new GUIContent("", "Move Layer Down"), GUI.skin.verticalScrollbarDownButton))
            {
                serializedLayerList.MoveArrayElement(layerIndex, layerIndex + 1);
                serializedLayerIndex.intValue++;
                //RenewActiveLayer();
                //changeMade = true;
                serializedObject.ApplyModifiedProperties();
                return true;
            }
        }
        return false;
    }

    protected bool DrawFolderMovementArrows(SerializedProperty serializedLayerList, SerializedProperty serializedLayerIndex)
    {
        Rect addUpRect = GetSlotRect(additionLeftHandleRect, 6, lineHeight, .2f);
        Rect addDownRect = GetSlotRect(additionLeftHandleRect, 6, lineHeight, .8f);

        if(parentEditorPreview != null)
        {
            if(layerIndex == 0)
            {
                if (GUI.Button(addUpRect, new GUIContent("", "Place Above Folder"), GUI.skin.verticalScrollbarUpButton))
                {
                    SerializedProperty newSerializedLayerHolder = parentEditorPreview.serializedLayerHolder;
                    SerializedProperty newSerializedLayerIndex = newSerializedLayerHolder.FindPropertyRelative("_activeLayerIndex");
                    SerializedProperty newSerializedLayerList = newSerializedLayerHolder.FindPropertyRelative("_layers");
                    IDrawingLayerHolder newLayerHolder = parentEditorPreview.layerHolder;
                    newSerializedLayerList.InsertArrayElementAtIndex(newSerializedLayerIndex.intValue);
                    //newSerializedLayerIndex.intValue--;
                    serializedLayerIndex.intValue = -1;

                    serializedObject.ApplyModifiedProperties();

                    newLayerHolder.layers[newSerializedLayerIndex.intValue] = layerHolder.RemoveLayerAtIndex(layerIndex);
                    return true;
                }
            }

            if(layerIndex == serializedLayerList.arraySize - 1)
            {
                if (GUI.Button(addDownRect, new GUIContent("", "Place Below Folder"), GUI.skin.verticalScrollbarDownButton))
                {
                    SerializedProperty newSerializedLayerHolder = parentEditorPreview.serializedLayerHolder;
                    SerializedProperty newSerializedLayerIndex = newSerializedLayerHolder.FindPropertyRelative("_activeLayerIndex");
                    SerializedProperty newSerializedLayerList = newSerializedLayerHolder.FindPropertyRelative("_layers");
                    IDrawingLayerHolder newLayerHolder = parentEditorPreview.layerHolder;
                    newSerializedLayerList.InsertArrayElementAtIndex(newSerializedLayerIndex.intValue + 1);
                    newSerializedLayerIndex.intValue++;
                    serializedLayerIndex.intValue = -1;

                    serializedObject.ApplyModifiedProperties();

                    newLayerHolder.layers[newSerializedLayerIndex.intValue] = layerHolder.RemoveLayerAtIndex(layerIndex);
                    return true;
                }
            }
        }

        if (layerIndex != 0)
        {
            if (layerHolder.layers[layerIndex - 1] is DrawingLayerFolder)
            {
                if (GUI.Button(addUpRect, new GUIContent("", "Add to Folder Above"), GUI.skin.verticalScrollbarUpButton))
                {
                    SerializedProperty newSerializedLayerHolder = serializedLayerList.GetArrayElementAtIndex(layerIndex - 1);
                    SerializedProperty newSerializedLayerIndex = newSerializedLayerHolder.FindPropertyRelative("_activeLayerIndex");
                    SerializedProperty newSerializedLayerList = newSerializedLayerHolder.FindPropertyRelative("_layers");
                    IDrawingLayerHolder newLayerHolder = layerHolder.layers[layerIndex - 1] as IDrawingLayerHolder;
                    newSerializedLayerIndex.intValue = newLayerHolder.layers.Count;
                    serializedLayerIndex.intValue--;
                    newSerializedLayerList.InsertArrayElementAtIndex(Mathf.Max(newSerializedLayerList.arraySize - 1, 0));

                    // open folder if something is added just for visibility
                    SerializedProperty openFolderProperty = newSerializedLayerHolder.FindPropertyRelative("_opened");
                    openFolderProperty.boolValue = true;

                    serializedObject.ApplyModifiedProperties();

                    newLayerHolder.layers[newLayerHolder.layers.Count - 1] = layerHolder.RemoveLayerAtIndex(layerIndex);
                    return true;
                }
            }
        }
        if (layerIndex < serializedLayerList.arraySize - 1)
        {
            if (layerHolder.layers[layerIndex + 1] is DrawingLayerFolder)
            {
                if (GUI.Button(addDownRect, new GUIContent("", "Add to Folder Below"), GUI.skin.verticalScrollbarDownButton))
                {
                    SerializedProperty newSerializedLayerHolder = serializedLayerList.GetArrayElementAtIndex(layerIndex + 1);
                    SerializedProperty newSerializedLayerIndex = newSerializedLayerHolder.FindPropertyRelative("_activeLayerIndex");
                    SerializedProperty newSerializedLayerList = newSerializedLayerHolder.FindPropertyRelative("_layers");
                    IDrawingLayerHolder newLayerHolder = layerHolder.layers[layerIndex + 1] as IDrawingLayerHolder;
                    newSerializedLayerIndex.intValue = 0;
                    newSerializedLayerList.InsertArrayElementAtIndex(0);
                    
                    // open folder if something is added just for visibility
                    SerializedProperty openFolderProperty = newSerializedLayerHolder.FindPropertyRelative("_opened");
                    openFolderProperty.boolValue = true;

                    serializedObject.ApplyModifiedProperties();

                    newLayerHolder.layers[0] = layerHolder.RemoveLayerAtIndex(layerIndex);
                    return true;
                }
            }
        }
        return false;
    }

    public void SendLayerIndexResetToParent()
    {
        //serializedObject.ApplyModifiedProperties();
        if (parentEditorPreview != null)
            parentEditorPreview.SendLayerIndexResetToParent();
        else
        {
            //layerHolder.ResetActiveLayerIndexAll();
            PropogateLayerIndexReset(layerHolder, serializedLayerHolder);
        }
    }

    private void PropogateLayerIndexReset(IDrawingLayerHolder currentHolder, SerializedProperty serializedCurrentHolder)
    {
        serializedCurrentHolder.FindPropertyRelative("_activeLayerIndex").intValue = -1;
        SerializedProperty serilaizedList = serializedCurrentHolder.FindPropertyRelative("_layers");
        int arraySize = serilaizedList.arraySize;
        for(int i = 0; i < arraySize; i++)
        {
            IDrawingLayer layer = currentHolder.layers[i];

            if (layer is IDrawingLayerHolder)
            {
                PropogateLayerIndexReset(layer as IDrawingLayerHolder, serilaizedList.GetArrayElementAtIndex(i));
            }
        }
    }

    public void SendLayerIndexUpward()
    {
        _serializedLayerHolder.FindPropertyRelative("_activeLayerIndex").intValue = layerIndex;
        if (parentEditorPreview != null)
            parentEditorPreview.SendLayerIndexUpward();
    }


    protected abstract void DrawThumbnail();

    /// <summary>
    /// Gets a rect for a new entry to be places at a specific elevation 
    /// </summary>
    /// <param name="parentRect"></param>
    /// <param name="size"></param>
    /// <param name="widthPadding"></param>
    /// <param name="slotLocation">0 - 1 where in parent from center of new Rect</param>
    /// <returns></returns>
    protected Rect GetSlotRect(Rect parentRect, float widthPadding, float height, float slotLocation)
    {
        slotLocation = Mathf.Clamp01(slotLocation);
        return new Rect(
            parentRect.x + (widthPadding / 2),
            (parentRect.y + (parentRect.height * slotLocation)) - (height / 2),
            parentRect.width - (widthPadding),
            height
            );
    }

    private void GetControlVals(out float xPos, out float yPos, out float width)
    {
        Rect initial = EditorGUILayout.GetControlRect();
        xPos = initial.x;
        yPos = initial.y;
        width = initial.width;
    }
}

public class DrawingLayerEditorPreviewTexture : DrawingLayerEditorPreview
{
    public DrawingLayerEditorPreviewTexture(SerializedObject serializedObject, SerializedProperty serializedLayerHolder, IDrawingLayerHolder layerHolder, int layerIndex, int indentLevel, DrawingLayerEditorPreview parent = null)
        : base(serializedObject, serializedLayerHolder, layerHolder, layerIndex, indentLevel, parent) {}

    public override bool DrawPreview()
    {
        if (base.DrawPreview())
            return true;

        DrawThumbnail();
        return false;
    }

    protected override void DrawThumbnail()
    {
        GUI.Box(thumbnailRect, (layerHolder.layers[layerIndex] as DrawingLayerTexture).outputTexture);
    }
}

public class DrawingLayerEditorPreviewFolder : DrawingLayerEditorPreview
{
    public DrawingLayerEditorPreviewFolder(SerializedObject serializedObject, SerializedProperty serializedLayerHolder, IDrawingLayerHolder layerHolder, int layerIndex, int indentLevel, DrawingLayerEditorPreview parent = null) 
        : base(serializedObject, serializedLayerHolder, layerHolder, layerIndex, indentLevel, parent) {}

    public override bool DrawPreview()
    {
        if (base.DrawPreview())
            return true;
        // find a better solution then just assigning these again here
        SerializedProperty serializedLayerList = serializedLayerHolder.FindPropertyRelative("_layers");
        SerializedProperty serializedLayer = serializedLayerList.GetArrayElementAtIndex(layerIndex);
        SerializedProperty serializedFolderToggle = serializedLayer.FindPropertyRelative("_opened");

        DrawThumbnail();
        DrawFolderToggle(serializedFolderToggle);
        if (DrawFolderLayers(serializedLayerList, serializedFolderToggle))
            return true;
       
        return false;
    }

    protected void DrawFolderToggle(SerializedProperty serializedFolderToggle)
    {
        Rect folderToggleRect = GetSlotRect(rightHandleRect, 6, lineHeight, .85f);
        serializedFolderToggle.boolValue = GUI.Toggle(
            folderToggleRect,
            serializedFolderToggle.boolValue,
            new GUIContent("", serializedFolderToggle.boolValue ? "Close Folder" : "Open Folder"),
            serializedFolderToggle.boolValue ? GUI.skin.verticalScrollbarDownButton : GUI.skin.horizontalScrollbarLeftButton
            );
        serializedObject.ApplyModifiedProperties();
    }
    protected bool DrawFolderLayers(SerializedProperty serializedLayerList, SerializedProperty serializedFolderToggle)
    {
        if (serializedFolderToggle.boolValue)
        {
            SerializedProperty newSerializedLayerHolder = serializedLayerList.GetArrayElementAtIndex(layerIndex);
            SerializedProperty newSerializedLayerList = newSerializedLayerHolder.FindPropertyRelative("_layers");
            IDrawingLayerHolder newLayerHolder = layerHolder.layers[layerIndex] as DrawingLayerFolder;

            for (int i = 0; i < newSerializedLayerList.arraySize; i++)
            {
                DrawingLayerEditorPreview preview = LayerHelperEditor.PreviewFromLayer(serializedObject, newSerializedLayerHolder, newLayerHolder, i, indentLevel + 1, this);
                if (preview.DrawPreview())
                    return true;
            }
        }
        return false;
    }
    protected override void DrawThumbnail()
    {
        GUI.Box(thumbnailRect, (layerHolder.layers[layerIndex] as DrawingLayerFolder).outputTexture);
    }
}

public static class LayerHelperEditor
{
    public static IDrawingLayerHolder TrueLayerHolder(IDrawingLayerHolder baseLayerHolder, SerializedProperty serializedBaseLayerHolder, out SerializedProperty serializedTrueLayerHolder)
    {
        serializedTrueLayerHolder = null;
        SerializedProperty serializedLayerIndex = serializedBaseLayerHolder.FindPropertyRelative("_activeLayerIndex");
        if(serializedLayerIndex.intValue < 0)
        {
            return null;
        }
        serializedTrueLayerHolder = serializedBaseLayerHolder;

        SerializedProperty serializedLayer = serializedBaseLayerHolder.FindPropertyRelative("_layers").GetArrayElementAtIndex(serializedLayerIndex.intValue);
        IDrawingLayer layer = baseLayerHolder.layers[serializedLayerIndex.intValue];
        if (layer is IDrawingLayerHolder)
        {
            IDrawingLayerHolder holder = layer as IDrawingLayerHolder;
            IDrawingLayerHolder childHolder = TrueLayerHolder(
                holder,
                serializedLayer,
                out SerializedProperty childSerializedTrueLayerHolder
                );
            if (childHolder == null)
            {
                return baseLayerHolder;
            }
            else
            {
                serializedTrueLayerHolder = childSerializedTrueLayerHolder;
                return childHolder;
            }
        }
        return baseLayerHolder;
    }
    public static DrawingLayerEditorPreview PreviewFromLayer(SerializedObject serializedObject, SerializedProperty serializedLayerHolder, IDrawingLayerHolder layerHolder, int layerIndex, int indentLevel, DrawingLayerEditorPreview parent = null)
    {
        IDrawingLayer layer = layerHolder.layers[layerIndex];
        DrawingLayerEditorPreview preview = null;

        switch (layer)
        {
            case DrawingLayerTexture drawingLayerTexture:
                preview = new DrawingLayerEditorPreviewTexture(serializedObject, serializedLayerHolder, layerHolder, layerIndex, indentLevel, parent);
                break;
            case DrawingLayerFolder drawingLayerFolder:
                preview = new DrawingLayerEditorPreviewFolder(serializedObject, serializedLayerHolder, layerHolder, layerIndex, indentLevel, parent);
                break;
        }

        return preview;
    }
}