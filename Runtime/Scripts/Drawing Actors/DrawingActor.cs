using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

[Serializable]
public abstract class DrawingActor
{
    // this needs to be saved so the correct type can be derived
    // from json serialization
    [SerializeField]
    protected DrawingActorType _typeEnum;
    public DrawingActorType typeEnum { get => _typeEnum; }
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
#if UNITY_EDITOR
    [SerializeField]
    protected string _tempActorTexturePath = "";
#endif
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

    public abstract void EditorTempInitialize();

    public abstract void EditorSave();
    public abstract void EditorTempSave();

    public abstract void DrawGUI(Vector4 margin);

    protected void EditorLoadTextureAtPath(string path)
    {
        if (path.Length > 0)
        {
            if (System.IO.File.Exists(path))
            {
                _actorTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            }
        }
    }
#endif
}