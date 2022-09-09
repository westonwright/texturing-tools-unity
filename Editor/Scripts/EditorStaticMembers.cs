using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public static class EditorStaticMembers
{
    private static readonly string texturesPath = "Textures/";

    private static readonly string transparentCheckerName = "transparent_checker";
    public static Texture2D transparentChecker;

    static EditorStaticMembers()
    {
        transparentChecker = Resources.Load<Texture2D>(texturesPath + transparentCheckerName);
    }
}
