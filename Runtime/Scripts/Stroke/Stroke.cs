using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class Stroke
{
    private DrawingActor _drawingActor;
    public DrawingActor drawingActor { get => _drawingActor; }

    private StrokeSegmentBuilder strokeSegmentBuilder;

    private Queue<StrokePoint> positionQueue;

    private StrokeSegment _currentSegment;
    public StrokeSegment currentSegment { get => _currentSegment; }

    // the current total length of the stroke at this time
    private float _lifetimeLength;
    public float lifetimeLength { get => _lifetimeLength; }

    // this is the unaltered (since last full stroke) texture
    // of the layer being drawn on
    private RenderTexture _refrenceTexture;
    public RenderTexture refrenceTexture { get => _refrenceTexture; }

    // this is the raw texture of the stroke that has been
    // generated so far on the drawing surface and is usually drawn in to
    private RenderTexture _strokeTexture;
    public RenderTexture strokeTexture { get => _strokeTexture; set => _strokeTexture = value; }

    // this is the resulting texture from applying the previous stroke point
    // not used by all brushes, but by smear/smudge for instance
    private RenderTexture _mixedTexture;
    public RenderTexture mixedTexture { get => _mixedTexture; set => _mixedTexture = value; }

    public Stroke(
        Vector2Int startingPixelPosition,
        RenderTexture refrenceToCopy,
        DrawingActor drawingActor
        )
    {
        positionQueue = new Queue<StrokePoint>();
        _currentSegment = new StrokeSegment(startingPixelPosition);
        _lifetimeLength = 0;
        this._drawingActor = drawingActor; // make this a copy?
        strokeSegmentBuilder = new StrokeSegmentBuilder(startingPixelPosition, this._drawingActor.spacing * this._drawingActor.size);
        this._refrenceTexture = TextureCalculations.DuplicateTexture(refrenceToCopy);
        this._strokeTexture = TextureCalculations.CreateEmptyTexture(new Vector2Int(refrenceToCopy.width, refrenceToCopy.height));
        this._mixedTexture = TextureCalculations.DuplicateTexture(refrenceToCopy);
        // because creating the stroke places a position without enqueueing it,
        // we have to update the stroke on creation
        UpdateStroke();
    }

    public void ContinuousEnqueue(Vector2Int pixelPosition)
    {
        // if we should take into consideration how far the cursor has traveled
        if (_drawingActor.spacingEnabled)
        {
            if(strokeSegmentBuilder.BuildFromPosition(pixelPosition, out List<Vector2Int> newPixelPositions))
            {
                foreach(Vector2Int pos in newPixelPositions)
                {
                    EnqueuePoint(pos, true);
                }
            }
        }
        else
        {
            strokeSegmentBuilder.SetPosition(pixelPosition);
            EnqueuePoint(pixelPosition, true);
        }
    }

    public void DiscontinuousEnqueue(Vector2Int pixelPosition)
    {
        strokeSegmentBuilder.SetPosition(pixelPosition);
        EnqueuePoint(pixelPosition, false);
    }

    /// <summary>
    /// Add a new position to the stroke's queue
    /// </summary>
    /// <param name="pixelPosition">The pixel position on the canvas to be drawn</param>
    /// <param name="continuous">If this position should be connected to the previous one or not. 
    /// eg. if a triangle wasn't connected </param>
    private void EnqueuePoint(Vector2Int pixelPosition, bool continuous = true)
    {
        positionQueue.Enqueue(new StrokePoint(pixelPosition, continuous));
    }
    
    /// <summary>
    /// Updates the current segment and lifetime length based on the next
    /// postion in the queue. Returns false if the queue is empty
    /// </summary>
    /// <returns></returns>
    public bool IterateStroke()
    {
        if(positionQueue.Count == 0)
        {
            return false;
        }
        else
        {
            StrokePoint strokePoint = positionQueue.Dequeue();
            if (strokePoint.continuous)
            {
                _currentSegment = new StrokeSegment(strokePoint.pixelPosition, _currentSegment);
                _lifetimeLength += _currentSegment.motionVector.magnitude;
            }
            else
            {
                _currentSegment = new StrokeSegment(strokePoint.pixelPosition);
            }
            UpdateStroke();
            return true;
        }
    }

    private void UpdateStroke()
    {
        _drawingActor.UpdateStroke(this);
    }

    public void ApplyStroke()
    {
        _drawingActor.ApplyStroke(this);
    }

    public void ReleaseTextures()
    {
        _strokeTexture.Release();
        _mixedTexture.Release();
        _refrenceTexture.Release();
    }
}
