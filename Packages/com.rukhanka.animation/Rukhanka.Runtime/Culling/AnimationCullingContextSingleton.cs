using System;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

/////////////////////////////////////////////////////////////////////////////////

namespace Rukhanka
{
public struct AnimationCullingContext: IComponentData, IDisposable
{
    public NativeList<float4> cullingPlanes;
    public NativeList<int2> cullingVolumePlaneRanges;
    
#if RUKHANKA_DEBUG_INFO
    public bool drawCullingVolumes;
    public uint cullingVolumeColor;
    
    public bool drawSceneBoundingBoxes;
    public uint visibleChunkColor;
    public uint invisibleChunkColor;
    public uint visibleRendererColor;
    public uint invisibleRendererColor;
#endif
    
    public void Dispose()
    {
        cullingPlanes.Dispose();
        cullingVolumePlaneRanges.Dispose();
    }
    
/////////////////////////////////////////////////////////////////////////////////

    internal void AddCullingPlanes(NativeArray<float4> planes)
    {
        if (planes.Length == 0)
            return;
        
        var volumePlaneRanges = new int2(cullingPlanes.Length, planes.Length);
        cullingVolumePlaneRanges.Add(volumePlaneRanges);
        cullingPlanes.AddRange(planes);
    }
}
}
