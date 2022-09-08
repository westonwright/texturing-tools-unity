using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class DrawingActorCalculations
{
    private static ComputeShader linearCircleCompute;
    private const string linearCircleComputeName = "LinearCircle";
    
    private static ComputeShader hardnessCompute;
    private const string hardnessComputeName = "Hardness";

    static DrawingActorCalculations()
    {
        string shapePath = DrawingStaticMembers.computeShaderPath + DrawingStaticMembers.computeShaderActorShapesPath;
        linearCircleCompute = Resources.Load<ComputeShader>(shapePath + linearCircleComputeName);
        hardnessCompute = Resources.Load<ComputeShader>(shapePath + hardnessComputeName);
    }

    public static RenderTexture Hardness(float hardness, RenderTexture inputTex)
    {
        hardnessCompute.SetTexture(0, "outputTex", inputTex);

        hardnessCompute.SetInt("brushSize", inputTex.width);
        hardnessCompute.SetFloat("hardness", hardness);
        hardnessCompute.Dispatch(0, HelperFunctions.xThreads(inputTex.width), HelperFunctions.yThreads(inputTex.width), 1);
        return inputTex;
    }

    public static RenderTexture LinearCircle(int size)
    {
        RenderTexture brushTex = TextureCalculations.CreateEmptyTexture(HelperFunctions.Num2Vec2Int(size, size));

        linearCircleCompute.SetTexture(0, "outputTex", brushTex);

        linearCircleCompute.SetInt("brushSize", size);
        linearCircleCompute.SetInt("brushRadius", size / 2);
        linearCircleCompute.Dispatch(0, HelperFunctions.xThreads(size), HelperFunctions.yThreads(size), 1);

        return brushTex;
    }

    /*
    [SerializeField]
    private ComputeShader standardBrushCompute;
    [SerializeField]
    private ComputeShader drawPointCompute;
    [SerializeField]
    private ComputeShader eraseTextureCompute;

    public RenderTexture UseBrush(Vector2Int pointPos, Vector2 pointDir, Brush brush, RenderTexture outputTexture)
    {
        for(int i = 0; i < Random.Range((int)(brush.count * (1 - brush.countJitter)), (int)brush.count + 1); i++)
        {
            int brushSize = (int)(brush.size * Random.Range(1 - brush.sizeJitter, 1));
            brushSize = brushSize <= 0 ? 1 : brushSize;
            //don't use randomized brush size for pointer position below because those randomizations should be independent of size randomization.
            //dividing brush size by 2 here just because it seems to make the scatter a bit too wide at its full value
            pointPos += HelperFunctions.Vec2ToVec2Int(HelperFunctions.Num2Vec2(-pointDir.y, pointDir.x) * (Random.Range(-brush.xScatter, brush.xScatter) * (brush.size / 2)));
            pointPos += HelperFunctions.Vec2ToVec2Int(pointDir * (Random.Range(-brush.yScatter, brush.yScatter) * (brush.size / 2)));
            Vector2Int brushPos = new Vector2Int(pointPos.x - (int)(brushSize / 2), pointPos.y - (int)(brushSize / 2));
            Vector2Int brushXYOffset = new Vector2Int(
                Mathf.Clamp(
                    brushPos.x - Mathf.Clamp(brushPos.x, 0, outputTexture.width - brushSize),
                    -brushSize,
                    brushSize
                    ),
                Mathf.Clamp(
                    brushPos.y - Mathf.Clamp(brushPos.y, 0, outputTexture.height - brushSize),
                    -brushSize,
                    brushSize
                    )
                );

            RectInt brushPixRect = new RectInt(Mathf.Clamp(brushPos.x, 0, outputTexture.width), Mathf.Clamp(brushPos.y, 0, outputTexture.height), brushSize - Mathf.Abs(brushXYOffset.x), brushSize - Mathf.Abs(brushXYOffset.y));

            if (brushPixRect.width > 0 && brushPixRect.height > 0)
            {
                RenderTexture brushTexture = TransformedBrushTexture(brush, sizeVar: brushSize);

                drawPointCompute.SetTexture(0, "outputTex", outputTexture);
                drawPointCompute.SetTexture(0, "brushAlpha", brushTexture);

                drawPointCompute.SetInts("brushRect", new int[] { brushPixRect.x, brushPixRect.y, brushPixRect.width, brushPixRect.height });
                drawPointCompute.SetInts("brushXYOffset", new int[] { brushXYOffset.x, brushXYOffset.y });
                drawPointCompute.SetFloats("brushColor", new float[] { brush.color.r, brush.color.g, brush.color.b, brush.color.a });

                drawPointCompute.Dispatch(0, HelperFunctions.xThreads(brushPixRect.width), HelperFunctions.yThreads(brushPixRect.height), 1);

                brushTexture.Release();
            }
        }
        return outputTexture;
    }

    public RenderTexture ErasePoint(RenderTexture strokeTexture, RenderTexture refrenceTexture, float opacity)
    {
        RenderTexture outputTex = TextureCalculations.DuplicateTexture(refrenceTexture);
        RenderTexture eraseTex = TextureCalculations.MultiplyTextureOpacity(TextureCalculations.DuplicateTexture(strokeTexture), opacity);

        eraseTextureCompute.SetTexture(0, "eraseTex", eraseTex);
        eraseTextureCompute.SetTexture(0, "outputTex", outputTex);
        eraseTextureCompute.SetInts("outputSize", new int[] { eraseTex.width, eraseTex.height });
        eraseTextureCompute.Dispatch(0, HelperFunctions.xThreads(eraseTex.width), HelperFunctions.yThreads(eraseTex.height), 1);

        eraseTex.Release();

        return outputTex;
    }

    public RenderTexture TransformedBrushTexture(
        Brush brush,
        int? sizeVar = null, 
        float? hardnessVar = null, 
        float? angleVar = null,
        float? textureRotationVar = null,
        float? roundnessVar = null,
        bool jitter = true
        )
    {

        //if brush is standard with hardness
        if (brush.brushTexture == null)
        {
            return CalculateStandardBrush(brush, sizeVar, hardnessVar, angleVar, roundnessVar, jitter);
        }
        //if brush has a texture
        else
        {
            int size = sizeVar ?? brush.size;
            float angle = angleVar ?? brush.angle;
            float textureRotation = textureRotationVar ?? brush.textureRotation;
            float roundness = roundnessVar ?? brush.roundness;
            if (jitter)
            {
                angle = (angle + Random.Range(-brush.angleJitter / 2, brush.angleJitter / 2));
                textureRotation = (textureRotation + Random.Range(-brush.textureRotationJitter / 2, brush.textureRotationJitter / 2));
                roundness = (roundness * Random.Range(1 - brush.roundnessJitter, 1));
            }

            int diameter = Mathf.CeilToInt(Mathf.Sqrt(Mathf.Pow(brush.brushTexture.width, 2) + Mathf.Pow(brush.brushTexture.height, 2)));

            RenderTexture transformedTex = TextureCalculations.Tex2D2RendTex(brush.brushTexture);

            transformedTex = TextureCalculations.ResizeCanvas(transformedTex, new Vector2Int(diameter, diameter));

            transformedTex = TextureCalculations.RotateTexture(transformedTex, textureRotation);

            transformedTex = TextureCalculations.ResizeTexture(transformedTex, new Vector2Int(Mathf.CeilToInt(diameter * roundness), diameter));

            transformedTex = TextureCalculations.ResizeCanvas(transformedTex, new Vector2Int(diameter, diameter));

            transformedTex = TextureCalculations.RotateTexture(transformedTex, angle);

            transformedTex = TextureCalculations.ResizeTexture(transformedTex, new Vector2Int(size, size));

            return transformedTex;
        }

    }

    public RenderTexture CalculateStandardBrush(
        Brush brush, 
        int? sizeVar = null, 
        float? hardnessVar = null, 
        float? angleVar = null, 
        float? roundnessVar = null,
        bool jitter = true
        )
    {
        int size = sizeVar ?? brush.size;
        float hardness = hardnessVar ?? brush.hardness;
        float angle = angleVar ?? brush.angle;
        float roundness = roundnessVar ?? brush.roundness;

        //size not included in jitter because its jitter is calculated earlier
        if (jitter)
        {
            hardness = (hardness * Random.Range(1 - brush.hardnessJitter, 1));
            angle = (angle + Random.Range(-brush.angleJitter / 2, brush.angleJitter / 2));
            roundness = (roundness * Random.Range(1 - brush.roundnessJitter, 1));
        }

        if (size == 0)
        {
            size = brush.size;
        }
        RenderTexture brushTex = TextureCalculations.CreateEmptyTexture(HelperFunctions.Num2Vec2Int(size, size));

        standardBrushCompute.SetTexture(0, "outputTex", brushTex);

        standardBrushCompute.SetInt("brushSize", size);
        standardBrushCompute.SetInt("brushRadius", size / 2);
        standardBrushCompute.SetFloat("brushHardness", hardness);
        standardBrushCompute.Dispatch(0, HelperFunctions.xThreads(size), HelperFunctions.yThreads(size), 1);
        
        brushTex = TextureCalculations.ResizeTexture(brushTex, new Vector2Int(Mathf.CeilToInt(size * roundness), size));

        brushTex = TextureCalculations.ResizeCanvas(brushTex, new Vector2Int(size, size));

        brushTex = TextureCalculations.RotateTexture(brushTex, angle);

        brushTex = TextureCalculations.ResizeTexture(brushTex, new Vector2Int(size, size));

        return brushTex;
    }
    */
}
