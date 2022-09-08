using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif
public abstract class DrawingActor
{
    protected static string computeShaderPath;
    static DrawingActor()
    {
        computeShaderPath = DrawingStaticMembers.computeShaderPath + DrawingStaticMembers.computeShaderActorBehaviorsPath;
    }

    [SerializeField]
    protected DrawingActorType _actorEnum;
    public DrawingActorType actorEnum { get => _actorEnum; }
    //measured in pixels
    //pixels are in refrence to pixels of the image on the plane, not pixels of the screen
    // this is diameter, not radius
    [SerializeField]
    protected int _size = 100;
    public virtual int size { get => _size; }

    //spacing is measured in pixels
    [SerializeField]
    protected bool _spacingEnabled = true;
    public virtual bool spacingEnabled { get => _spacingEnabled; }

    [SerializeField]
    protected float _spacing = .5f;
    public virtual float spacing { get => _spacing; }

    [SerializeField]
    protected string _actorTexturePath = "";
    protected Texture2D _actorTexture = null;

    public DrawingActor() { }

    // takes in stroke data and applies its stroke to the appropriate textures provided in stroke data
    // smudge should apply changes directly to the mixed texture
    // erase should create a new mixed texture from stroke and refrence
    public abstract void UpdateStroke(Stroke strokeData);
    // this is what the texture for a single point will be after 
    // and transformations and jitters are appliedd
    // if preview is true, wont apply jitter and stuff
    public abstract void ApplyStroke(Stroke strokeData);
    protected abstract RenderTexture TransformedDrawingActorTexture(int outputSize, bool preview = false);
    //protected abstract RenderTexture TransformedActorOutline();

#if UNITY_EDITOR
    public abstract void EditorInitialize();
    public abstract void EditorSave();

    public abstract void DrawGUI(Vector4 margin);
#endif
}

public enum DrawingActorType
{
    brush,
    pencil,
    eraser
}

public class DrawingActorSelector
{
    [SerializeField]
    DrawingActorType drawingActorType;

    public DrawingActor DrawingActor(string json)
    {
        switch (drawingActorType)
        {
            case DrawingActorType.brush:
                return JsonUtility.FromJson<DrawingActorBrush>(json);
            case DrawingActorType.pencil:
                return JsonUtility.FromJson<DrawingActorBrush>(json);
                //return JsonUtility.FromJson<DrawingActorPencil>(json);
            case DrawingActorType.eraser:
                return JsonUtility.FromJson<DrawingActorBrush>(json);
                //return JsonUtility.FromJson<DrawingActorEraser>(json);
        }
        return new DrawingActorBrush();
    }
}