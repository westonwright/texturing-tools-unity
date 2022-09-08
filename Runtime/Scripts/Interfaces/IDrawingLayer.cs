using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IDrawingLayer
{
    public DrawingLayerType layerType { get; }
    public bool drawable { get; }
    public string name { get; }
    public bool visible { get; }
    public bool locked { get; }
    //public float opacity { get; }
    //public bool solo { get; }
    public RenderTexture outputTexture { get; }

    public void HardReset();
    public void ReleaseOutput();

    public void BeforeDestroy();
}
