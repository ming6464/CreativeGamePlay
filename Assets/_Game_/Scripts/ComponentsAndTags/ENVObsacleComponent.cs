using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

public enum ENVObstacleShape
{
    Rectangle,
    Circle,
    Triangle
}

public struct ENVObstacleInfo : IComponentData
{
    public ENVObstacleShape shape;
    public LocalTransform   lt;
    public float3 center;
    public quaternion rotation;
    public float radius;
    public float length;
    public float width;
}