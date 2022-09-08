using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

[Serializable]
public class DrawingLayerTexture : IDrawingLayer, ISerializedTexture
{
    public DrawingLayerType layerType { get => DrawingLayerType.Texture; }

    public bool drawable { get => true; }

    [SerializeField]
    [HideInInspector]
    private string _name = "New Texture";
    public string name { get => _name; }
    [SerializeField]
    [HideInInspector]
    private bool _visible = true;
    public bool visible { get => _visible; }
    [SerializeField]
    private bool _locked = false;
    public bool locked { get => _locked; }

    [SerializeField]
    [HideInInspector]
    private TextureBytesSO bytesSO;

    [NonSerialized]
    private RenderTexture _baseTexture = null; // being null might be a problem
    public RenderTexture baseTexture { get => _baseTexture; }
    [NonSerialized]
    private RenderTexture _outputTexture = null;
    public RenderTexture outputTexture
    {
        get
        {
            if (_outputTexture == null)
            {
                _outputTexture = CalculateOutputTexture(baseTexture);
            }
            return _outputTexture;
        }
    }

    // add list of effects to be applied

    //creat enum for blend mode?
    public void InitializeTexture(Vector2Int resolution)
    {
        //Debug.Log("Initializing on layer");
        if (bytesSO == null || bytesSO.bytes.Length == 0)
        {
            _baseTexture = TextureCalculations.CreateEmptyTexture(resolution);
        }
        else
        {
            Texture2D tex = new Texture2D(resolution.x, resolution.y, TextureFormat.RGBA32, false);
            tex.LoadRawTextureData(bytesSO.bytes);
            tex.Apply();
            _baseTexture = TextureCalculations.Tex2D2RendTex(tex);
        }

        _outputTexture = CalculateOutputTexture(baseTexture);
    }

    public void HardReset()
    {
        if (bytesSO != null)
        {
            bytesSO = null;
        }
        _visible = true;
        _name = "New Texture";
    }

    public ScriptableObject[] ActiveSOs()
    {
        if (bytesSO != null)
        {
            return new ScriptableObject[] { bytesSO };
        }
        else return new ScriptableObject[0];
    }

    public ScriptableObject[] AllSOs()
    {
        return ActiveSOs();
    }

    public void ChangeResolution(Vector2Int resolution)
    {
        _baseTexture = TextureCalculations.ResizeTexture(_baseTexture, resolution);
        _outputTexture = CalculateOutputTexture(_baseTexture);
        SaveBytes();
    }

    public RenderTexture ApplyLayerToTemporary(RenderTexture inputTexture)
    {
        return CalculateOutputTexture(inputTexture);
    }

    public void CopyToBaseTexture(RenderTexture inputTexture)
    {
        ReleaseOutput();
        _baseTexture = TextureCalculations.DuplicateTexture(inputTexture);
        _outputTexture = CalculateOutputTexture(_baseTexture);
        SaveBytes();
    }

    public void ReleaseBase()
    {
        if (_baseTexture != null)
        {
            _baseTexture.Release();
        }
        _baseTexture = null;
    }

    public void ReleaseOutput()
    {

        if (_outputTexture != null)
        {
            _outputTexture.Release();
        }
        _outputTexture = null;
    }

    private RenderTexture CalculateOutputTexture(RenderTexture inputTexture)
    {
        // actually do things here like apply opacity etc.
        return TextureCalculations.DuplicateTexture(inputTexture);
    }

    private void SaveBytes()
    {
        //Debug.Log("Saving Bytes on Layer");
        if (baseTexture == null) return;
        if (bytesSO == null) bytesSO = ScriptableObject.CreateInstance<TextureBytesSO>();
        bytesSO.bytes = TextureCalculations.RendTexToTex2D(baseTexture).GetRawTextureData();
    }

    public void BeforeDestroy()
    {
        ReleaseBase();
        ReleaseOutput();
    }
}
