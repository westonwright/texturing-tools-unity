using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class TextureCalculations
{
    private static ComputeShader resizeTextureCompute;
    private const string resizeTextureComputeName = "ResizeTexture";
    private static ComputeShader resizeCanvasCompute;
    private const string resizeCanvasComputeName = "ResizeCanvas";
    private static ComputeShader halveTextureCompute;
    private const string halveTextureComputeName = "HalveTexture";
    private static ComputeShader rotateTextureCompute;
    private const string rotateTextureComputeName = "RotateTexture";
    private static ComputeShader combineTexturesCompute;
    private const string combineTexturesComputeName = "CombineTextures";
    private static ComputeShader clearTextureCompute;
    private const string clearTextureComputeName = "ClearTexture";
    private static ComputeShader multiplyTextureOpacityCompute;
    private const string multiplyTextureOpacityComputeName = "MultiplyTextureOpacity";
    private static ComputeShader changeTextureOpacityCompute;
    private const string changeTextureOpacityComputeName = "ChangeTextureOpacity";
    private static ComputeShader changeTextureColorCompute;
    private const string changeTextureColorComputeName = "ChangeTextureColor";

    static TextureCalculations()
    {
        string path = DrawingStaticMembers.computeShaderPath + DrawingStaticMembers.computeShaderTextureCalculationsPath;
        resizeTextureCompute = Resources.Load<ComputeShader>(path + resizeTextureComputeName);
        resizeCanvasCompute = Resources.Load<ComputeShader>(path + resizeCanvasComputeName);
        halveTextureCompute = Resources.Load<ComputeShader>(path + halveTextureComputeName);
        rotateTextureCompute = Resources.Load<ComputeShader>(path + rotateTextureComputeName);
        combineTexturesCompute = Resources.Load<ComputeShader>(path + combineTexturesComputeName);
        clearTextureCompute = Resources.Load<ComputeShader>(path + clearTextureComputeName);
        multiplyTextureOpacityCompute = Resources.Load<ComputeShader>(path + multiplyTextureOpacityComputeName);
        changeTextureOpacityCompute = Resources.Load<ComputeShader>(path + changeTextureOpacityComputeName);
        changeTextureColorCompute = Resources.Load<ComputeShader>(path + changeTextureColorComputeName);
    }

    #region texture functions

    public static RenderTexture ResizeTexture(RenderTexture inputTex, Vector2Int targetSize)
    {
        //keeps dividing inputTex by half until it is as close as possible to target size of output
        //this is done to make the bilinear mpping look better.
        while (true)
        {
            bool divideX = targetSize.x <= 4 ? false : inputTex.width / 2 < targetSize.x ? false : true;
            bool divideY = targetSize.y <= 4 ? false : inputTex.height / 2 < targetSize.y ? false : true;

            if (divideX || divideY)
            {
                inputTex = HalveTexture(inputTex, divideX, divideY);
            }
            else
            {
                break;
            }
        }

        RenderTexture outputTex = CreateEmptyTexture(targetSize);

        resizeTextureCompute.SetTexture(0, "inputTex", inputTex);
        resizeTextureCompute.SetTexture(0, "outputTex", outputTex);

        resizeTextureCompute.SetInts("outputSize", new int[] { outputTex.width, outputTex.height });
        resizeTextureCompute.SetFloats("sizeRatio", new float[] { (float)inputTex.width / outputTex.width, (float)inputTex.height / outputTex.height });

        resizeTextureCompute.Dispatch(0, HelperFunctions.xThreads(outputTex.width), HelperFunctions.yThreads(outputTex.height), 1);

        inputTex.Release();

        return outputTex;
    }

    public static RenderTexture HalveTexture(RenderTexture inputTex, bool halveX, bool halveY)
    {
        RenderTexture outputTex = CreateEmptyTexture(new Vector2Int(halveX ? inputTex.width / 2 : inputTex.width, halveY ? inputTex.height / 2 : inputTex.height));

        halveTextureCompute.SetTexture(0, "inputTex", inputTex);
        halveTextureCompute.SetTexture(0, "outputTex", outputTex);

        halveTextureCompute.SetInts("outputSize", new int[] { outputTex.width, outputTex.height });
        halveTextureCompute.SetBool("halveX", halveX);
        halveTextureCompute.SetBool("halveY", halveY);

        halveTextureCompute.Dispatch(0, HelperFunctions.xThreads(outputTex.width), HelperFunctions.yThreads(outputTex.height), 1);

        inputTex.Release();

        return outputTex;
    }

    public static RenderTexture ResizeCanvas(RenderTexture inputTex, Vector2Int targetSize)
    {
        RenderTexture outputTex = CreateEmptyTexture(targetSize);

        resizeCanvasCompute.SetTexture(0, "inputTex", inputTex);
        resizeCanvasCompute.SetTexture(0, "outputTex", outputTex);

        resizeCanvasCompute.SetInts("inputSize", new int[] { inputTex.width, inputTex.height });
        resizeCanvasCompute.SetInts("outputSize", new int[] { outputTex.width, outputTex.height });

        Vector2Int xyOffset = new Vector2Int((outputTex.width - inputTex.width) / 2, (outputTex.height - inputTex.height) / 2);
        resizeCanvasCompute.SetInts("xyOffset", new int[] { xyOffset.x, xyOffset.y });

        resizeCanvasCompute.Dispatch(0, HelperFunctions.xThreads(outputTex.width), HelperFunctions.yThreads(outputTex.height), 1);

        inputTex.Release();

        return outputTex;
    }

    public static RenderTexture RotateTexture(RenderTexture inputTex, float rotDegs)
    {
        RenderTexture outputTex = CreateEmptyTexture(HelperFunctions.Num2Vec2Int(inputTex.width, inputTex.height));

        rotateTextureCompute.SetTexture(0, "inputTex", inputTex);
        rotateTextureCompute.SetTexture(0, "outputTex", outputTex);

        rotateTextureCompute.SetInts("size", new int[] { inputTex.width, inputTex.height });
        rotateTextureCompute.SetFloat("rotRads", Mathf.Deg2Rad * rotDegs);

        rotateTextureCompute.Dispatch(0, HelperFunctions.xThreads(inputTex.width), HelperFunctions.yThreads(inputTex.height), 1);

        inputTex.Release();

        return outputTex;
    }

    /// <summary>
    /// Merges two textures into a new Texture. Does not release either input texture.
    /// </summary>
    /// <param name="bottomTex"></param>
    /// <param name="topTex"></param>
    /// <returns></returns>
    public static RenderTexture MergeTexturesToNew(RenderTexture bottomTex, RenderTexture topTex)
    {
        RenderTexture outputTex = DuplicateTexture(bottomTex);
        combineTexturesCompute.SetTexture(0, "topTex", topTex);
        combineTexturesCompute.SetTexture(0, "bottomTex", outputTex);

        combineTexturesCompute.SetInts("topTexSize", new int[] { topTex.width, topTex.height });
        combineTexturesCompute.SetInts("bottomTexSize", new int[] { outputTex.width, outputTex.height });
        combineTexturesCompute.SetFloats("sizeRatio", new float[] { topTex.width / outputTex.width, topTex.height / outputTex.height });

        combineTexturesCompute.Dispatch(0, HelperFunctions.xThreads(outputTex.width), HelperFunctions.yThreads(outputTex.height), 1);

        //dont think i need to release the toptex because i might not want it deleted right here
        //topTex.Release();

        return outputTex;
    }

    /// <summary>
    /// Merges two textures into the bottom of the two. Does not release the top texture
    /// </summary>
    /// <param name="bottomTex"></param>
    /// <param name="topTex"></param>
    /// <returns></returns>
    public static RenderTexture MergeTexturesToBottom(RenderTexture bottomTex, RenderTexture topTex)
    {
        combineTexturesCompute.SetTexture(0, "topTex", topTex);
        combineTexturesCompute.SetTexture(0, "bottomTex", bottomTex);

        combineTexturesCompute.SetInts("topTexSize", new int[] { topTex.width, topTex.height });
        combineTexturesCompute.SetInts("bottomTexSize", new int[] { bottomTex.width, bottomTex.height });
        combineTexturesCompute.SetFloats("sizeRatio", new float[] { topTex.width / bottomTex.width, topTex.height / bottomTex.height });

        combineTexturesCompute.Dispatch(0, HelperFunctions.xThreads(bottomTex.width), HelperFunctions.yThreads(bottomTex.height), 1);

        //dont think i need to release the toptex because i might not want it deleted right here
        //topTex.Release();

        return bottomTex;
    }

    public static RenderTexture MultiplyTextureOpacity(RenderTexture tex, float opacityVal)
    {
        multiplyTextureOpacityCompute.SetTexture(0, "tex", tex);

        multiplyTextureOpacityCompute.SetInts("texSize", new int[] { tex.width, tex.height });
        multiplyTextureOpacityCompute.SetFloat("brushOpacity", opacityVal);

        multiplyTextureOpacityCompute.Dispatch(0, HelperFunctions.xThreads(tex.width), HelperFunctions.yThreads(tex.height), 1);

        return tex;
    }
    public static RenderTexture ChangeTextureOpacity(RenderTexture tex, float opacityVal)
    {
        changeTextureOpacityCompute.SetTexture(0, "tex", tex);

        changeTextureOpacityCompute.SetInts("texSize", new int[] { tex.width, tex.height });
        changeTextureOpacityCompute.SetFloat("opacity", opacityVal);

        changeTextureOpacityCompute.Dispatch(0, HelperFunctions.xThreads(tex.width), HelperFunctions.yThreads(tex.height), 1);

        return tex;
    }

    public static RenderTexture ChangeTextureColor(RenderTexture tex, Color color)
    {
        changeTextureColorCompute.SetTexture(0, "tex", tex);

        changeTextureColorCompute.SetInts("texSize", new int[] { tex.width, tex.height });
        changeTextureColorCompute.SetFloats("newColor", new float[] { color.r, color.g, color.b});

        changeTextureColorCompute.Dispatch(0, HelperFunctions.xThreads(tex.width), HelperFunctions.yThreads(tex.height), 1);

        return tex;
    }

    public static RenderTexture ClearTexture(RenderTexture tex)
    {
        clearTextureCompute.SetTexture(0, "tex", tex);

        clearTextureCompute.Dispatch(0, HelperFunctions.xThreads(tex.width), HelperFunctions.yThreads(tex.height), 1);

        return tex;
    }

    public static RenderTexture CreateEmptyTexture(Vector2Int size)
    {
        RenderTexture outputTex = new RenderTexture(size.x, size.y, 0);
        outputTex.enableRandomWrite = true;
        //not sure if i should or shouldn't set aa
        outputTex.antiAliasing = 1;
        outputTex.filterMode = FilterMode.Point;
        //outputTex.antiAliasing = 4;
        outputTex.Create();

        return outputTex;
    }

    public static RenderTexture TransferTexture(RenderTexture inputTex, RenderTexture outputTex)
    {
        //might add functionality to automatically resize texrues if they don't match when transfering
        Graphics.CopyTexture(inputTex, outputTex);

        inputTex.Release();

        return outputTex;
    }

    public static RenderTexture DuplicateTexture(RenderTexture inputTex)
    {
        RenderTexture outputTex = CreateEmptyTexture(new Vector2Int(inputTex.width, inputTex.height));

        Graphics.CopyTexture(inputTex, outputTex);

        return outputTex;
    }

    public static Texture2D RendTexToTex2D(RenderTexture rendTex)
    {
        Texture2D tex2D = new Texture2D(rendTex.width, rendTex.height, TextureFormat.RGBA32, false);
        RenderTexture.active = rendTex;
        tex2D.ReadPixels(new Rect(0, 0, rendTex.width, rendTex.height), 0, 0);
        RenderTexture.active = null;
        tex2D.Apply();
        return tex2D;
    }

    public static RenderTexture Tex2D2RendTex(Texture2D tex2D)
    {
        RenderTexture rendTex = CreateEmptyTexture(new Vector2Int(tex2D.width, tex2D.height));
        RenderTexture.active = rendTex;
        Graphics.Blit(tex2D, rendTex);
        RenderTexture.active = null;
        return rendTex;
    }

    #endregion
}
