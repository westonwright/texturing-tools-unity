using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class LayerHelper
{
    // could change switch statements from type to enum?
    public static void InitializeLayers(IList<IDrawingLayer> layers, Vector2Int resolution)
    {
        foreach(IDrawingLayer layer in layers)
        {
            switch (layer)
            {
                case DrawingLayerTexture drawingLayerTexture:
                    drawingLayerTexture.InitializeTexture(resolution);
                    break;
                case DrawingLayerFolder drawingLayerFolder:
                    drawingLayerFolder.Initialize(resolution);
                    break;
            }
        }
    }

    public static Stroke CreateStroke(IDrawingLayer layer, Vector2Int pixelCoord)
    {
        if (layer == null) return null;
        switch (layer)
        {
            case DrawingLayerTexture drawingLayerTexture:
                return new Stroke(pixelCoord, drawingLayerTexture.baseTexture, DrawingActorStream.drawingActor);
            case DrawingLayerFolder drawingLayerFolder:
                return CreateStroke(drawingLayerFolder.activeLayer, pixelCoord);
        }
        return null;
    }

    public static RenderTexture FlattenLayerTextures(IList<IDrawingLayer> layers, Vector2Int resolution)
    {
        RenderTexture flattenedTex = TextureCalculations.CreateEmptyTexture(resolution);
        foreach(IDrawingLayer layer in layers)
        {
            if (!layer.visible)
                continue;

            RenderTexture tempTex = null;
            tempTex = TextureCalculations.MergeTexturesToNew(flattenedTex, layer.outputTexture);
            flattenedTex.Release();
            flattenedTex = tempTex;
            /*
            switch (layer)
            {
                case DrawingLayerTexture drawingLayerTexture:
                    RenderTexture tempTex = TextureCalculations.MergeTexturesToNew(flattenedTex, layer.outputTexture);
                    flattenedTex.Release();
                    flattenedTex = tempTex;
                    break;
                case DrawingLayerFolder drawingLayerFolder:
                    RenderTexture tempTex = TextureCalculations.MergeTexturesToNew(flattenedTex, layer.outputTexture);
                    flattenedTex.Release();
                    flattenedTex = tempTex;
                    break;
            }
            */
        }

        return flattenedTex;
    }

    public static RenderTexture FlattenLayerAndStrokeTextures(IList<IDrawingLayer> layers, Vector2Int resolution, int activeLayerIndex, Stroke stroke)
    {
        RenderTexture flattenedTex = TextureCalculations.CreateEmptyTexture(resolution);
        for(int i = 0; i < layers.Count; i++)
        {
            if (!layers[i].visible)
                continue;

            switch (layers[i])
            {
                case DrawingLayerTexture drawingLayerTexture:
                    RenderTexture layerTexture = i == activeLayerIndex ?
                        drawingLayerTexture.ApplyLayerToTemporary(stroke.mixedTexture) :
                        drawingLayerTexture.outputTexture;

                    flattenedTex = TextureCalculations.MergeTexturesToBottom(flattenedTex, layerTexture);
                    if (i == activeLayerIndex) layerTexture.Release();
                    break;
                case DrawingLayerFolder drawingLayerFolder:
                    RenderTexture folderTexture = i == activeLayerIndex ? 
                        FlattenLayerAndStrokeTextures(drawingLayerFolder.layers, resolution, drawingLayerFolder.activeLayerIndex, stroke) :
                        drawingLayerFolder.outputTexture;

                    flattenedTex = TextureCalculations.MergeTexturesToBottom(flattenedTex, folderTexture);
                    if (i == activeLayerIndex) folderTexture.Release();
                    break;
            }
        }
        return flattenedTex;
    }
    
    public static void ApplyStrokeToLayer(IDrawingLayer layer, Stroke stroke)
    {
        if (layer == null) return;
        switch (layer)
        {
            case DrawingLayerTexture drawingLayerTexture:
                drawingLayerTexture.CopyToBaseTexture(stroke.mixedTexture);
                break;
            case DrawingLayerFolder drawingLayerFolder:
                ApplyStrokeToLayer(drawingLayerFolder.activeLayer, stroke);
                break;
        }

    }

    public static void ReleaseAll(IList<IDrawingLayer> layers)
    {
        foreach(IDrawingLayer layer in layers)
        {
            layer.ReleaseOutput();

            if(layer is ISerializedTexture)
            {
                (layer as ISerializedTexture).ReleaseBase();
            }
            if (layer is IDrawingLayerHolder)
            {
                (layer as IDrawingLayerHolder).ReleaseAll();
            }
        }
    }
}
