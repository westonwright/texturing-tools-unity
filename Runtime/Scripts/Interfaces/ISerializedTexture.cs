using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface ISerializedTexture : IActiveSO
{
    public RenderTexture baseTexture { get; }

    public void InitializeTexture(Vector2Int resolution);

    public void ChangeResolution(Vector2Int resolution);

    public RenderTexture ApplyLayerToTemporary(RenderTexture inputTexture);

    public void CopyToBaseTexture(RenderTexture inputTexture);

    public void ReleaseBase();
}
