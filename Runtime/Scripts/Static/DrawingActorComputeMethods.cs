using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class DrawingActorComputeMethods
{
    // eventually make these private and just call methods instead
    private static readonly string computeShaderPath;

    public static ComputeShader brushPointCompute;
    private static readonly string brushPointComputeName = "BrushPoint";
    public static ComputeShader eraserStrokeCompute;
    private static readonly string eraserStrokeName = "EraserStroke";

    static DrawingActorComputeMethods()
    {
        computeShaderPath = DrawingStaticMembers.computeShaderPath + DrawingStaticMembers.computeShaderActorBehaviorsPath;
        brushPointCompute = Resources.Load<ComputeShader>(computeShaderPath + brushPointComputeName);
        eraserStrokeCompute = Resources.Load<ComputeShader>(computeShaderPath + eraserStrokeName);
    }
}
