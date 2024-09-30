using UnityEngine;
using Unity.Mathematics;

public static class DebugDraw
{
    public static void DrawBox(float3 center, float3 halfExtents, quaternion orientation, float3 direction, Color color, float maxDistance = 0, float duration = 0)
    {
        // Calculate the 8 vertices of the box
        float3[] vertices = new float3[8];
        vertices[0] = center + math.mul(orientation, new float3(-halfExtents.x, -halfExtents.y, -halfExtents.z));
        vertices[1] = center + math.mul(orientation, new float3(halfExtents.x, -halfExtents.y, -halfExtents.z));
        vertices[2] = center + math.mul(orientation, new float3(halfExtents.x, -halfExtents.y, halfExtents.z));
        vertices[3] = center + math.mul(orientation, new float3(-halfExtents.x, -halfExtents.y, halfExtents.z));
        vertices[4] = center + math.mul(orientation, new float3(-halfExtents.x, halfExtents.y, -halfExtents.z));
        vertices[5] = center + math.mul(orientation, new float3(halfExtents.x, halfExtents.y, -halfExtents.z));
        vertices[6] = center + math.mul(orientation, new float3(halfExtents.x, halfExtents.y, halfExtents.z));
        vertices[7] = center + math.mul(orientation, new float3(-halfExtents.x, halfExtents.y, halfExtents.z));

        // Draw the 12 edges of the box
        Debug.DrawLine(vertices[0], vertices[1], color, duration);
        Debug.DrawLine(vertices[1], vertices[2], color, duration);
        Debug.DrawLine(vertices[2], vertices[3], color, duration);
        Debug.DrawLine(vertices[3], vertices[0], color, duration);
        Debug.DrawLine(vertices[4], vertices[5], color, duration);
        Debug.DrawLine(vertices[5], vertices[6], color, duration);
        Debug.DrawLine(vertices[6], vertices[7], color, duration);
        Debug.DrawLine(vertices[7], vertices[4], color, duration);
        Debug.DrawLine(vertices[0], vertices[4], color, duration);
        Debug.DrawLine(vertices[1], vertices[5], color, duration);
        Debug.DrawLine(vertices[2], vertices[6], color, duration);
        Debug.DrawLine(vertices[3], vertices[7], color, duration);

        // Draw the direction ray
        Debug.DrawRay(center, direction * maxDistance, color, duration);
    }
    public static void DrawWireSphere(float3 center, float radius, Color color, float duration = 0)
    {
        int   segments  = 20;
        float angle     = 0;
        float increment = math.PI * 2.0f / segments;

        float3 lastPoint = center + new float3(math.cos(angle), math.sin(angle), 0) * radius;
        for (int i = 1; i <= segments; i++)
        {
            angle += increment;
            float3 nextPoint = center + new float3(math.cos(angle), math.sin(angle), 0) * radius;
            Debug.DrawLine(lastPoint, nextPoint, color, duration);
            lastPoint = nextPoint;
        }

        lastPoint = center + new float3(math.cos(angle), 0, math.sin(angle)) * radius;
        for (int i = 1; i <= segments; i++)
        {
            angle += increment;
            float3 nextPoint = center + new float3(math.cos(angle), 0, math.sin(angle)) * radius;
            Debug.DrawLine(lastPoint, nextPoint, color, duration);
            lastPoint = nextPoint;
        }

        lastPoint = center + new float3(0, math.cos(angle), math.sin(angle)) * radius;
        for (int i = 1; i <= segments; i++)
        {
            angle += increment;
            float3 nextPoint = center + new float3(0, math.cos(angle), math.sin(angle)) * radius;
            Debug.DrawLine(lastPoint, nextPoint, color, duration);
            lastPoint = nextPoint;
        }
    }
    
}