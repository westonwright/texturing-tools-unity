using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

[Serializable]
public class DrawingChannel : IDrawingLayerHolder
{
    [SerializeField]
    private string _name = "_MainTex";
    public string name { get => _name; }

    [SerializeField]
    private Vector2Int _resolution = new Vector2Int(1024, 1024);
    public Vector2Int resolution { get => _resolution; }

    [SerializeReference]
    private List<IDrawingLayer> _layers = new List<IDrawingLayer>() { };
    public List<IDrawingLayer> layers { get => _layers; }
    public IDrawingLayer activeLayer
    {
        get
        {
            if (activeLayerIndex == -1 || activeLayerIndex >= _layers.Count)
                return null;
            else
                return _layers[activeLayerIndex];
        }
    }
    public IDrawingLayer deepestActiveLayer
    {
        get
        {
            IDrawingLayer active = activeLayer;
            if (active == null)
                return null;
            if (active is IDrawingLayerHolder)
            {
                IDrawingLayer trueActive = (active as IDrawingLayerHolder).deepestActiveLayer;
                if (trueActive == null)
                    return active;
                else
                    return trueActive;
            }
            else
                return active;
        }
    }
    public IList<IDrawingLayer> activeLayerHierarchy
    {
        get
        {
            IDrawingLayer active = activeLayer;
            List<IDrawingLayer> activeList = new List<IDrawingLayer>();
            if (active == null)
                return activeList;

            activeList.Add(active);
            if (active is IDrawingLayerHolder)
            {
                foreach (IDrawingLayer layer in (active as IDrawingLayerHolder).activeLayerHierarchy)
                {
                    activeList.Add(layer);
                }
            }
            return activeList;
        }
    }

    [SerializeField]
    private int _activeLayerIndex = -1;
    public int activeLayerIndex { get => _activeLayerIndex; }

    [SerializeField]
    private FilterMode filterMode = FilterMode.Point;

    // option for color mode? 
    // if switch gets made, we need to confirm with a popup
    // because that will mean destructively copying to a new type
    //[SerializeField]
    //private RenderTextureFormat _textureFormat = RenderTextureFormat.ARGB32;

    // might be able to get rid of this waiting variable?
    private bool _waiting = false;
    public bool waiting { get => _waiting; }

    private RenderTexture _outputTexture;
    public RenderTexture outputTexture { get => _outputTexture; }

    // Move stroke, instant mode, and uv(?) options out of this.
    // maybe move it into drawing stream? or perhaps into the surface
    // if false, only draws a maximum of one point per frame and queues the rest
    // previously known as slow mode but renamed because of ambiguity
    // instant mode can cause lag and reduce the sample rate
    [SerializeField]
    private bool instantMode = false;

    // not sure if i need a coroutine because this is just used when finishing the stroke
    // and that can all just happen in one frame unless we want it to look pretty
    //Coroutine strokeCoroutine;
    private Stroke stroke = null;

    // TODO?: option to loop uv. Basically like if smudinging on the edge
    // of the texture, the smudge will be applied to both looping edges

    public DrawingChannel() { }
    /*
    public DrawingChannel(Vector2Int resolution)
    {
        this.resolution = resolution;
        _layers[0].InitializeTexture(this.resolution);
    }
    */

    // might make it where we can initialize a single layer instead of all of them?
    public void Initialize()
    {
        LayerHelper.InitializeLayers(layers, _resolution);
        SafeReleaseOutputTexture();
        _outputTexture = LayerHelper.FlattenLayerTextures(layers, this._resolution);
    }

    /// <summary>
    /// When layers are duplicated, we need to clean them so that
    /// they do not share the same data
    /// </summary>
    public void CreateNewLayerInPlace(DrawingLayerType layerType)
    {
        if (layers.Count < 1) return;

        switch (layerType)
        {
            case DrawingLayerType.Texture:
                layers[0] = new DrawingLayerTexture();
                break;
            case DrawingLayerType.Folder:
                layers[0] = new DrawingLayerFolder();
                break;
        }
    }

    public bool UsesSpacing()
    {
        if (stroke != null)
            return stroke.drawingActor.spacingEnabled;
        else
            return false;
    }

    public ScriptableObject[] ActiveSOs()
    {
        List<ScriptableObject> SOs = new List<ScriptableObject>();
        // this could be more later but for now
        // this is all we need
        if(activeLayerIndex >= 0 && activeLayerIndex < _layers.Count)
        {
            if (activeLayer is IActiveSO)
            {
                foreach (ScriptableObject SO in (activeLayer as IActiveSO).AllSOs())
                {
                    SOs.Add(SO);
                }
            }
        }
        return SOs.ToArray();
    }

    public ScriptableObject[] AllSOs()
    {
        List<ScriptableObject> SOs = new List<ScriptableObject>();
        foreach (IDrawingLayer layer in _layers)
        {
            // if it has hactive scriptable objects
            if(layer is IActiveSO)
            {
                foreach (ScriptableObject SO in (layer as IActiveSO).AllSOs())
                {
                    SOs.Add(SO);
                }
            }
        }
        return SOs.ToArray();
    }

    public void ChangeResolution(Vector2Int resolution)
    {
        // make sure this cant run if we are currently drawing
        // or basically if a stroke exists
        this._resolution = resolution;
        // dont think i need to resize output since it gets released immediately anyway
        //_outputTexture = TextureCalculations.ResizeTexture(_outputTexture, newResolution);
        foreach(IDrawingLayer layer in layers)
        {
            if(layer is ISerializedTexture)
            {
                (layer as ISerializedTexture).ChangeResolution(this._resolution);
            }
        }
        SafeReleaseOutputTexture();
        _outputTexture = LayerHelper.FlattenLayerTextures(layers, this._resolution);
        // reset stroke too?
    }

    // continues the stroke on a line based on the previous posiiton
    public void SetStrokeContinuous(Vector2 uvCoords)
    {
        if(SetStroke(uvCoords, out Vector2Int curPixelCoord))
        {
            stroke.ContinuousEnqueue(curPixelCoord);
        }
    }
    public void SetStrokeContinuous(Vector2Int pixelCoord)
    {
        if (SetStroke(pixelCoord))
        {
            stroke.ContinuousEnqueue(pixelCoord);
        }
    }

    // continues the stroke as though it were a new stroke but keeps it under the same action
    public void SetStrokeDiscontinuous(Vector2 canvasCoord)
    {
        if (SetStroke(canvasCoord, out Vector2Int curPixelCoord))
        {
            stroke.DiscontinuousEnqueue(curPixelCoord);
        }
    }
    public void SetStrokeDiscontinuous(Vector2Int pixelCoord)
    {
        if (SetStroke(pixelCoord))
        {
            stroke.DiscontinuousEnqueue(pixelCoord);
        }
    }

    private bool SetStroke(Vector2 canvasCoord, out Vector2Int pixelCoord)
    {
        pixelCoord = CanvasToPixelCoord(canvasCoord);

        return StrokeCreated(pixelCoord);
    }
    private bool SetStroke(Vector2Int pixelCoord)
    {
        return StrokeCreated(pixelCoord);
    }

    private bool StrokeCreated(Vector2Int pixelCoord)
    {
        bool trueLocked = false;
        foreach(IDrawingLayer layer in activeLayerHierarchy)
        {
            if (layer.locked || !layer.visible)
            {
                trueLocked = true;
                break;
            }
        }
        if (activeLayer == null || !activeLayer.drawable) return false;
        if (trueLocked) return false;
        // start a new stroke if one doesnt exist
        if (stroke == null)
        {
            stroke = LayerHelper.CreateStroke(activeLayer, pixelCoord);

            // if stroke is still null
            if (stroke == null)
            {
                return false;
            }

            if (!instantMode)
                _waiting = true;

            TempApplyStroke();
            return false;
        }
        else
        {
            return true;
        }
    }

    public void FinishStroke()
    {
        if (stroke != null)
        {
            // this finishes drawing the strokes queued positions
            // could move to a coroutine if we want it to look prettier
            while (stroke.IterateStroke())
            {
                // do the stuff
            }
            FinalApplyStroke();
            _waiting = false;
        }
    }

    // called to actually make the stroke draw itself
    public bool IterateStroke()
    {
        if(stroke != null)
        {
            if (stroke.IterateStroke())
            {
                if (instantMode)
                {
                    // does any more iterations left
                    while (stroke.IterateStroke()) { }
                }
                TempApplyStroke();
                return true;
            }
        }
        return false;
    }

    private void FinalApplyStroke()
    {
        stroke.ApplyStroke();
        LayerHelper.ApplyStrokeToLayer(activeLayer, stroke);
        ResetStroke();
        SafeReleaseOutputTexture();
        _outputTexture = LayerHelper.FlattenLayerTextures(layers, _resolution);
    }

    private void TempApplyStroke()
    {
        stroke.ApplyStroke();
        SafeReleaseOutputTexture();
        _outputTexture = LayerHelper.FlattenLayerAndStrokeTextures(layers, _resolution, activeLayerIndex, stroke);
    }

    private void SafeReleaseOutputTexture()
    {
        if(outputTexture != null)
        {
            outputTexture.Release();
        }
    }

    public void ResetActiveLayerIndexAll()
    {
        _activeLayerIndex = -1;
        foreach (IDrawingLayer layer in _layers)
        {
            if (layer is IDrawingLayerHolder)
            {
                (layer as IDrawingLayerHolder).ResetActiveLayerIndexAll();
            }
        }
    }

    public void ReleaseAll()
    {
        SafeReleaseOutputTexture();
        LayerHelper.ReleaseAll(layers);
        ResetStroke();
    }

    public IDrawingLayer RemoveLayerAtIndex(int index)
    {
        IDrawingLayer layer = _layers[index];
        _layers.RemoveAt(index);
        return layer;
    }

    private void ResetStroke()
    {
        if(stroke != null)
        {
            stroke.ReleaseTextures();
            stroke = null;
        }
    }
    private Vector2Int CanvasToPixelCoord(Vector2 canvasCoord)
    {
        return new Vector2Int((int)(_resolution.x * canvasCoord.x), (int)(_resolution.y * canvasCoord.y));
    }

    public void BeforeDestroy()
    {
        ReleaseAll();
    }
}
