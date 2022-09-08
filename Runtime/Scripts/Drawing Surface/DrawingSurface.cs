using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEditor;

// ask if collider should be automatically removed?
[ExecuteAlways]
[RequireComponent(typeof(MeshRenderer), typeof(MeshFilter), typeof(MeshCollider))]
public class DrawingSurface : MonoBehaviour
{
    private Mesh _surfaceMesh;
    public Mesh surfaceMesh { get => _surfaceMesh; }
    private Material _surfaceMaterial;
    public Material surfaceMaterial { get => _surfaceMaterial; }

    private Vector2[] prevUVs;
    private int prevTriIndex;
    private Vector2 prevScreenPixelCoords;

    [SerializeReference]
    [HideInInspector]
    private List<DrawingChannel> drawingChannels = new List<DrawingChannel>() { new DrawingChannel() };
    public DrawingChannel activeChannel
    {
        get
        {
            if (activeChannelIndex < drawingChannels.Count)
                return drawingChannels[activeChannelIndex];
            else 
                return null; // theres probably a better answer than this
        }
    }

    [SerializeField]
    [HideInInspector]
    private int activeChannelIndex = 0;

    #region initialization
    void Start()
    {
        GetRequiredComponents();
        Initialize();
    }

    public void GetRequiredComponents()
    {
        _surfaceMaterial = GetComponent<MeshRenderer>().sharedMaterial;
        _surfaceMesh = GetComponent<MeshFilter>().sharedMesh;
    }

    public void Initialize()
    {
        // TODO: fix the initialization
        foreach(DrawingChannel channel in drawingChannels)
        {
            channel.Initialize();
        }

        _surfaceMaterial.SetTexture(activeChannel.name, activeChannel.outputTexture);
    }
    #endregion

    #region events/update

    // Returns true if the pointer hit the object
    public bool PointerDown(Vector3 screenPixelCoords)
    {
        prevScreenPixelCoords = screenPixelCoords;
        Ray pointerRay = Camera.current.ScreenPointToRay(prevScreenPixelCoords);
        RaycastHit hit;

        if (Physics.Raycast(pointerRay, out hit))
        {
            if (hit.transform.gameObject == this.gameObject)
            {
                int curTriIndex = hit.triangleIndex;
                Vector2[] curUVs = new Vector2[] { _surfaceMesh.uv[_surfaceMesh.triangles[curTriIndex * 3]], _surfaceMesh.uv[_surfaceMesh.triangles[curTriIndex * 3 + 1]], _surfaceMesh.uv[_surfaceMesh.triangles[curTriIndex * 3 + 2]] };

                prevTriIndex = curTriIndex;
                prevUVs = curUVs;
                // discontinuous because it is the first point
                activeChannel.SetStrokeDiscontinuous(hit.textureCoord);
                TempApplyStroke();
                return true;
            }
        }
        return false;
    }

    public void PointerDrag(Vector2Int screenPixelCoords)
    {
        if (screenPixelCoords != prevScreenPixelCoords)
        {
            if (activeChannel.UsesSpacing())
            {
                float positionDist = Vector2.Distance(screenPixelCoords, prevScreenPixelCoords);

                float lerpStep = 1 / positionDist;
                float currentLerp = 0;

                while (true)
                {
                    //this might break if the initial distance is less than one?
                    if (currentLerp > 1)
                    {
                        break;
                    }
                    Vector2 screenSamplePos = Vector2.Lerp(prevScreenPixelCoords, screenPixelCoords, currentLerp);
                    Ray pointerRay = Camera.current.ScreenPointToRay(screenSamplePos);
                    currentLerp += lerpStep;

                    RaycastHit hit;
                    if (Physics.Raycast(pointerRay, out hit))
                    {
                        if (hit.transform.gameObject == this.gameObject)
                        {
                            int curTriIndex = hit.triangleIndex;

                            if (curTriIndex != prevTriIndex)
                            {
                                Vector2[] curUVs = new Vector2[] { _surfaceMesh.uv[_surfaceMesh.triangles[curTriIndex * 3]], _surfaceMesh.uv[_surfaceMesh.triangles[curTriIndex * 3 + 1]], _surfaceMesh.uv[_surfaceMesh.triangles[curTriIndex * 3 + 2]] };

                                int matchCount = 0;
                                foreach (Vector2 curUV in curUVs)
                                {
                                    foreach (Vector2 prevUV in prevUVs)
                                    {
                                        if (curUV == prevUV)
                                        {
                                            matchCount++;
                                        }
                                    }
                                }
                                prevUVs = curUVs;

                                //match count of less than 2 means uv is not connected, this should not be interpolated
                                //match count of 2 means uv is connected, this should be interpolated
                                //match count of 3 means uv is the same, this should not be interpolated
                                //Debug.Log("diff tri " + matchCount + " " + screenSamplePos);
                                if (matchCount != 2)
                                    activeChannel.SetStrokeDiscontinuous(hit.textureCoord);
                                else
                                    activeChannel.SetStrokeContinuous(hit.textureCoord);
                            }
                            else
                            {
                                //Debug.Log("same tri");
                                activeChannel.SetStrokeContinuous(hit.textureCoord);
                            }
                            // need to move apply stroke somewhere so it can continue
                            // to update when not in instant mode
                            prevTriIndex = curTriIndex;
                            TempApplyStroke();
                        }
                    }
                }
            }
            else
            {
                Ray pointerRay = Camera.current.ScreenPointToRay(HelperFunctions.Vec2IntToVec2(screenPixelCoords));
                RaycastHit hit;
                if (Physics.Raycast(pointerRay, out hit))
                {
                    if (hit.transform.gameObject == this.gameObject)
                    {
                        int curTriIndex = hit.triangleIndex;

                        if (curTriIndex != prevTriIndex)
                        {
                            prevUVs = new Vector2[] { _surfaceMesh.uv[_surfaceMesh.triangles[prevTriIndex * 3]], _surfaceMesh.uv[_surfaceMesh.triangles[prevTriIndex * 3 + 1]], _surfaceMesh.uv[_surfaceMesh.triangles[prevTriIndex * 3 + 2]] };
                        }
                        // need to move apply stroke somewhere so it can continue
                        // to update when not in instant mode
                        prevTriIndex = curTriIndex;
                        activeChannel.SetStrokeDiscontinuous(hit.textureCoord);
                        TempApplyStroke();
                    }
                }
            }

        }
        prevScreenPixelCoords = screenPixelCoords;
    }

    public void PointerUp()
    {
        FinalApplyStroke();
    }
    private void TempApplyStroke()
    {
        activeChannel.IterateStroke();
        _surfaceMaterial.SetTexture(activeChannel.name, activeChannel.outputTexture);
    }

    private void FinalApplyStroke()
    {
        activeChannel.FinishStroke();
        _surfaceMaterial.SetTexture(activeChannel.name, activeChannel.outputTexture);
    }
    // TODO: implement undo and redo

    #endregion

    #region saving

    public void SaveTexture(RenderTexture rendTex, string fullPath)
    {
        Texture2D tex2D = TextureCalculations.RendTexToTex2D(rendTex);
        var data = tex2D.EncodeToPNG();
        Debug.Log(fullPath);
        System.IO.File.WriteAllBytes(fullPath, data);
    }
    #endregion
}
