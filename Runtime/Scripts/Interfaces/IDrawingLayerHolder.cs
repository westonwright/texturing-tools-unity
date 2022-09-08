using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IDrawingLayerHolder : IActiveSO
{
    public List<IDrawingLayer> layers { get; }
    public int activeLayerIndex { get; }
    public IDrawingLayer activeLayer { get; }
    public IDrawingLayer deepestActiveLayer { get; }
    public IList<IDrawingLayer> activeLayerHierarchy { get; }
    public RenderTexture outputTexture { get; }
    public void ChangeResolution(Vector2Int resolution);
    // Resets the active layer index on this and all holder children
    public void ResetActiveLayerIndexAll();
    public void ReleaseAll();
    public IDrawingLayer RemoveLayerAtIndex(int index);
}
