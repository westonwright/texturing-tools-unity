using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

[Serializable]
public class DrawingLayerFolder : IDrawingLayer, IDrawingLayerHolder
{
    public DrawingLayerType layerType { get => DrawingLayerType.Folder; }
    public bool drawable { get => true; }

    [SerializeField]
    private string _name = "New Folder";
    public string name { get => _name; }
    [SerializeField]
    private bool _visible = true;
    public bool visible { get => _visible; }

    [SerializeField]
    private bool _locked = false;
    public bool locked { get => _locked; }

    [SerializeField]
    private bool _opened = true;

    [SerializeReference]
    private List<IDrawingLayer> _layers = new List<IDrawingLayer>();
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

    private Vector2Int resolution = new Vector2Int(1024, 1024);

    private RenderTexture _outputTexture;
    public RenderTexture outputTexture { 
        get 
        {
            if (_outputTexture == null)
            {
                _outputTexture = TextureCalculations.CreateEmptyTexture(resolution);
            }
            return _outputTexture;
        } 
    }


    [SerializeField]
    private int _activeLayerIndex = -1;
    public int activeLayerIndex { get => _activeLayerIndex; }

    public void Initialize(Vector2Int resolution)
    {
        this.resolution = resolution;
        LayerHelper.InitializeLayers(layers, this.resolution);
        ReleaseOutput();
        _outputTexture = LayerHelper.FlattenLayerTextures(layers, this.resolution);
    }

    public ScriptableObject[] ActiveSOs()
    {
        List<ScriptableObject> SOs = new List<ScriptableObject>();
        if(activeLayerIndex >= 0 && activeLayerIndex < _layers.Count)
        {
            if (_layers[activeLayerIndex] is IActiveSO)
            {
                foreach(ScriptableObject SO in (_layers[activeLayerIndex] as IActiveSO).ActiveSOs())
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
            if (layer is IActiveSO)
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
        this.resolution = resolution;
        // dont think i need to resize output since it gets released immediately anyway
        //_outputTexture = TextureCalculations.ResizeTexture(_outputTexture, newResolution);
        foreach (IDrawingLayer layer in layers)
        {
            if (layer is ISerializedTexture)
            {
                (layer as ISerializedTexture).ChangeResolution(this.resolution);
            }
        }
        ReleaseOutput();
        _outputTexture = LayerHelper.FlattenLayerTextures(layers, this.resolution);
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
        ReleaseOutput();
        LayerHelper.ReleaseAll(layers);
    }

    public void ReleaseOutput()
    {
        if (_outputTexture != null)
        {
            _outputTexture.Release();
        }
        _outputTexture = null;
    }

    public void HardReset()
    {
        _name = "New Folder";
        _visible = true;
        _layers = new List<IDrawingLayer>();
    }

    public IDrawingLayer RemoveLayerAtIndex(int index)
    {
        IDrawingLayer layer = _layers[index];
        _layers.RemoveAt(index);
        //_layers[index] = null;
        return layer;
    }

    public void BeforeDestroy()
    {
        ReleaseAll();
        LayerHelper.ReleaseAll(layers);
    }
}
