using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class DrawingStaticMembers
{
    public const int threadGroupSize = 8;
    //public const string texturePath = "Textures/";
    public const string computeShaderPath = "Compute/";
    public const string computeShaderTextureCalculationsPath = "Texture Calculations/";
    public const string computeShaderActorBehaviorsPath = "Drawing Actor Behaviors/";
    public const string computeShaderActorShapesPath = "Drawing Actor Shapes/";
    //private const string defaultBrushTextureName = "brush_default_texture";

    static DrawingStaticMembers()
    {
        //defaultBrushTexture = Resources.Load<Texture2D>(texturePath + defaultBrushTextureName);
    }
}
