using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StrokeSegmentBuilder
{
    private Vector2Int prevPixelPosition;
    private float segmentLength;
    private readonly float thresholdLength;

    public StrokeSegmentBuilder(Vector2Int prevPixelPosition, float thresholdLength)
    {
        this.prevPixelPosition = prevPixelPosition;
        this.segmentLength = 0;
        this.thresholdLength = thresholdLength;
    }
    
    public void SetPosition(Vector2Int pixelPosition)
    {
        prevPixelPosition = pixelPosition;
        segmentLength = 0;
    }

    public bool BuildFromPosition(Vector2Int pixelPosition, out List<Vector2Int> newPixelPositions)
    {
        newPixelPositions = new List<Vector2Int>();
        
        float extendedLength = segmentLength + Vector2.Distance(pixelPosition, prevPixelPosition);
        float distanceToThreshold = thresholdLength - segmentLength;
        while (extendedLength >= thresholdLength)
        {
            Vector2 direction = pixelPosition - prevPixelPosition;
            Vector2Int newPixelPosition = prevPixelPosition + HelperFunctions.Vec2ToVec2Int(direction.normalized * distanceToThreshold);
            newPixelPositions.Add(newPixelPosition);
            prevPixelPosition = newPixelPosition;
            extendedLength -= thresholdLength;
            if(segmentLength > 0)
            {
                distanceToThreshold = thresholdLength;
                segmentLength = 0;
            }
        }
        segmentLength = extendedLength;
        prevPixelPosition = pixelPosition;

        if (newPixelPositions.Count > 0) return true;
        else return false;
    }
}

