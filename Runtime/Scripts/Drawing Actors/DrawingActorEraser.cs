using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

[System.Serializable]
public class DrawingActorEraser : DrawingActor
{
    [SerializeField]
    private float _sizeJitter = 0;
    //disable hardness for non-standard brushes
    [SerializeField]
    private float _hardness = 0;
    [SerializeField]
    private float _hardnessJitter = 0;
    //texture rotation is how to texture should be rotated before it is squished.
    //disable texture rotation for standard brushes
    [SerializeField]
    private float _textureRotation = 0;
    [SerializeField]
    private float _textureRotationJitter = 0;
    //squish is from 0 to 1. 0 is full square / circle, 1 is completely flat
    [SerializeField]
    private float _width = 1;
    [SerializeField]
    private float _widthJitter = 0;
    //rotation is measured in degrees
    //brush rotation is additional rottaion applied to the texture after it has been squished and rotated by itself
    [SerializeField]
    private float _angle = 0;
    [SerializeField]
    private float _angleJitter = 0;
    [SerializeField]
    private float _xScatter = 0;
    [SerializeField]
    private float _yScatter = 0;
    // x spread and y spread?
    [SerializeField]
    private int _count = 1;
    [SerializeField]
    private float _countJitter = 0;
    [SerializeField]
    private Color _color = new Color(1, 0, 0, 1);

    public DrawingActorEraser()
    {
        _typeEnum = DrawingActorType.Eraser;
    }

    public override void UpdateStroke(Stroke stroke)
    {
        for (int i = 0; i < Random.Range((int)(_count * (1 - _countJitter)), (int)_count + 1); i++)
        {
            // multiply by 1.41421f to account for texture downsizing when rotating
            int brushSize = (int)((size * 1.41421f) * Random.Range(1 - _sizeJitter, 1));
            brushSize = brushSize <= 0 ? 1 : brushSize;
            //don't use randomized brush size for pointer position below because those randomizations should be independent of size randomization.
            //dividing brush size by 2 here just because it seems to make the scatter a bit too wide at its full value
            Vector2Int pointPos = stroke.currentSegment.prevPixelPosition + stroke.currentSegment.motionVector;
            Vector2 directionVector = ((Vector2)stroke.currentSegment.motionVector).normalized;
            pointPos += HelperFunctions.Vec2ToVec2Int(HelperFunctions.Num2Vec2(-directionVector.y, directionVector.x) * (Random.Range(-_xScatter, _xScatter) * (_size / 2)));
            pointPos += HelperFunctions.Vec2ToVec2Int(directionVector * (Random.Range(-_yScatter, _yScatter) * (_size / 2)));
            Vector2Int brushPos = new Vector2Int(pointPos.x - (int)(brushSize / 2f), pointPos.y - (int)(brushSize / 2f));
            // not sure about this calculation for offset
            // mainly the texture width/height part
            Vector2Int brushXYOffset = new Vector2Int(
                Mathf.Clamp(
                    brushPos.x - Mathf.Clamp(brushPos.x, 0, stroke.strokeTexture.width - brushSize),
                    -brushSize,
                    brushSize
                    ),
                Mathf.Clamp(
                    brushPos.y - Mathf.Clamp(brushPos.y, 0, stroke.strokeTexture.height - brushSize),
                    -brushSize,
                    brushSize
                    )
                );

            RectInt brushPixRect = new RectInt(Mathf.Clamp(brushPos.x, 0, stroke.strokeTexture.width), Mathf.Clamp(brushPos.y, 0, stroke.strokeTexture.height), brushSize - Mathf.Abs(brushXYOffset.x), brushSize - Mathf.Abs(brushXYOffset.y));

            if (brushPixRect.width > 0 && brushPixRect.height > 0)
            {
                RenderTexture brushTexture = TransformedDrawingActorTexture(brushSize);

                DrawingActorComputeMethods.brushPointCompute.SetTexture(0, "outputTex", stroke.strokeTexture);
                DrawingActorComputeMethods.brushPointCompute.SetTexture(0, "brushAlpha", brushTexture);

                DrawingActorComputeMethods.brushPointCompute.SetInts("brushRect", new int[] { brushPixRect.x, brushPixRect.y, brushPixRect.width, brushPixRect.height });
                DrawingActorComputeMethods.brushPointCompute.SetInts("brushXYOffset", new int[] { brushXYOffset.x, brushXYOffset.y });
                DrawingActorComputeMethods.brushPointCompute.SetFloats("brushColor", new float[] { _color.r, _color.g, _color.b, _color.a });

                DrawingActorComputeMethods.brushPointCompute.Dispatch(0, HelperFunctions.xThreads(brushPixRect.width), HelperFunctions.yThreads(brushPixRect.height), 1);

                brushTexture.Release();
            }
        }   
    }

    // eraser stroke is drawn like a normal brush
    // then the stroke's alpha is inverted for final erasing
    // could update to erase using color data as well
    public override void ApplyStroke(Stroke stroke)
    {
        RenderTexture correctedStroke = TextureCalculations.MultiplyTextureOpacity(TextureCalculations.DuplicateTexture(stroke.strokeTexture), _color.a);
        RenderTexture outputTexture = TextureCalculations.DuplicateTexture(stroke.refrenceTexture);

        DrawingActorComputeMethods.eraserStrokeCompute.SetTexture(0, "eraseTex", correctedStroke);
        DrawingActorComputeMethods.eraserStrokeCompute.SetTexture(0, "outputTex", outputTexture);

        DrawingActorComputeMethods.eraserStrokeCompute.SetInts("outputSize", new int[] { outputTexture.width, outputTexture.height });

        DrawingActorComputeMethods.eraserStrokeCompute.Dispatch(0, HelperFunctions.xThreads(outputTexture.width), HelperFunctions.yThreads(outputTexture.height), 1);

        stroke.mixedTexture.Release();
        stroke.mixedTexture = outputTexture;
        correctedStroke.Release();
    }

    protected override RenderTexture TransformedDrawingActorTexture(int outputSize, bool preview = false)
    {
        // calculate default texture if one hasnt been chosen
        RenderTexture transformedTex = _actorTexture != null ?
            TextureCalculations.Tex2D2RendTex(_actorTexture) :
            DrawingActorCalculations.LinearCircle(size);

        float finalAngle = _angle;
        float finalTexutreRotation = _textureRotation;
        float finalWidth = _width;
        float finalHardness = _hardness;
        if (!preview)
        {
            finalAngle = (finalAngle + Random.Range(-_angleJitter / 2f, _angleJitter / 2f));
            finalTexutreRotation = (finalTexutreRotation + Random.Range(-_textureRotationJitter / 2, _textureRotationJitter / 2));
            finalWidth = (finalWidth * Random.Range(1 - _widthJitter, 1));
            finalHardness = (finalHardness * Random.Range(1 - _hardnessJitter, 1));
        }

        int diameter = Mathf.CeilToInt(Mathf.Sqrt(Mathf.Pow(transformedTex.width, 2) + Mathf.Pow(transformedTex.height, 2)));

        transformedTex = DrawingActorCalculations.Hardness(finalHardness, transformedTex);

        transformedTex = TextureCalculations.ResizeCanvas(transformedTex, new Vector2Int(diameter, diameter));

        transformedTex = TextureCalculations.RotateTexture(transformedTex, finalTexutreRotation);

        transformedTex = TextureCalculations.ResizeTexture(transformedTex, new Vector2Int(Mathf.CeilToInt(diameter * finalWidth), diameter));

        transformedTex = TextureCalculations.ResizeCanvas(transformedTex, new Vector2Int(diameter, diameter));

        transformedTex = TextureCalculations.RotateTexture(transformedTex, finalAngle);

        transformedTex = TextureCalculations.ResizeTexture(transformedTex, new Vector2Int(outputSize, outputSize));

        return transformedTex;
    }

#if UNITY_EDITOR
    public override void EditorInitialize()
    {
        EditorLoadTextureAtPath(_actorTexturePath);
        _tempActorTexturePath = _actorTexturePath;
    }

    public override void EditorTempInitialize()
    {
        EditorLoadTextureAtPath(_tempActorTexturePath);
    }

    public override void EditorSave()
    {

        if (_actorTexture != null) _actorTexturePath = AssetDatabase.GetAssetPath(_actorTexture);
        else _actorTexturePath = "";
        _tempActorTexturePath = _actorTexturePath;
    }

    public override void EditorTempSave()
    {
        if (_actorTexture != null) _tempActorTexturePath = AssetDatabase.GetAssetPath(_actorTexture);
        else _tempActorTexturePath = "";
    }

    public override void DrawGUI(Vector4 margin)
    {
        EditorGUILayout.BeginHorizontal();
            EditorGUILayout.BeginVertical();
                EditorGUILayout.LabelField("Eraser Texture:", GUILayout.Width(120));
                Texture2D _tempTexture = _actorTexture;
                _tempTexture = (Texture2D)EditorGUILayout.ObjectField(_tempTexture, typeof(Texture2D), false, GUILayout.Width(120), GUILayout.Height(120));
                if(_tempTexture != _actorTexture)
                {
                    _actorTexture = _tempTexture;
                    EditorTempSave();
                }      
                //if (actorTexture == null) actorTexture = DrawingStaticMembers.defaultBrushTexture;
                EditorWindowHelper.DrawSlider(ref _color.a, "Opacity", 0f, 1f);

            EditorGUILayout.EndVertical();
            GUILayout.FlexibleSpace();
            // sliders
            EditorGUILayout.BeginVertical();
                EditorWindowHelper.DrawSlider(ref _size, "Size", 4, 1000);
                EditorWindowHelper.DrawSlider(ref _width, "Width", .01f, 1f);
                EditorWindowHelper.DrawSlider(ref _angle, "Angle", 0, 359);
                EditorWindowHelper.DrawSlider(ref _textureRotation, "Texture Rotation", 0, 359);
                EditorWindowHelper.DrawSlider(ref _hardness, "Hardness", 0f, 1f);
                EditorWindowHelper.DrawSlider(ref _sizeJitter, "Size Jitter", 0f, 1f);
                EditorWindowHelper.DrawSlider(ref _widthJitter, "Width Jitter", 0f, 1f);
                EditorWindowHelper.DrawSlider(ref _angleJitter, "Angle Jitter", 0f, 359f);
                EditorWindowHelper.DrawSlider(ref _textureRotationJitter, "Texture Rotation Jitter", 0f, 359f);
                EditorWindowHelper.DrawSlider(ref _hardnessJitter, "Hardness Jitter", 0f, 1f);
            EditorGUILayout.EndVertical();           
            GUILayout.FlexibleSpace();
            // texture preview with color
            EditorGUILayout.BeginVertical();
                EditorGUILayout.LabelField("Eraser Preview:", GUILayout.Width(100));

                Rect r = GUILayoutUtility.GetRect(120, 120);
                r.width = 120;
                r.height = 120;
                GUI.backgroundColor = Color.black;
                GUI.Box(r, "");
                GUI.backgroundColor = Color.white;

                // dont just draw actor texture here, calculate the correct transformed texture
                RenderTexture pointPreview = TransformedDrawingActorTexture((int)r.width, true);
                GUI.DrawTexture(r, pointPreview);
                //GUI.DrawTexture(r, actorTexture);
                pointPreview.Release();

                EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField("Enable Spacing:", GUILayout.Width(100));
                    _spacingEnabled = EditorGUILayout.Toggle(spacingEnabled);
                EditorGUILayout.EndHorizontal();
                EditorGUI.BeginDisabledGroup(!_spacingEnabled);
                EditorWindowHelper.DrawSlider(ref _spacing, "Spacing", .01f, 1f);
                EditorGUI.EndDisabledGroup();
                EditorWindowHelper.DrawSlider(ref _xScatter, "X Scatter", 0f, 10f);
                EditorWindowHelper.DrawSlider(ref _yScatter, "Y Scatter", 0f, 10f);
                EditorWindowHelper.DrawSlider(ref _count, "Count", 1, 10);
                EditorWindowHelper.DrawSlider(ref _countJitter, "Count Jitter", 0f, 1f);
            EditorGUILayout.EndVertical();
        EditorGUILayout.EndHorizontal();
        Rect previewRect = GUILayoutUtility.GetRect(350, 1920, 120, 1080);
        GUI.backgroundColor = Color.black;
        GUI.Box(previewRect, "");
        GUI.backgroundColor = Color.white;

        if(previewRect.width > 1)
        {
            RenderTexture strokePreview = EditorWindowHelper.DrawStrokePreview(new Vector2Int((int)previewRect.width, (int)previewRect.height), this);
            GUI.DrawTexture(previewRect, strokePreview);
            //Debug.Log(lol.width);
            strokePreview.Release();
        }
    }
#endif
}