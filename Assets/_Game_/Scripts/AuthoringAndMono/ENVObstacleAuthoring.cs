using System;
using Unity.Entities;
using Unity.Transforms;
using UnityEngine;

namespace _Game_.Scripts.AuthoringAndMono
{
    public class ENVObstacleAuthoring : MonoBehaviour
    {
        public ENVObstacleShape shape;
        public float            radius = 5;
        public float            length = 5;
        public float            width = 5;

        [Header("EDIT")]
        public Color Color  = Color.yellow;

        public float height = 1f;
        private class ENVObstacleAuthoringBaker : Baker<ENVObstacleAuthoring>
        {
            public override void Bake(ENVObstacleAuthoring authoring)
            {
                Entity entity = GetEntity(TransformUsageFlags.Dynamic);
                LocalTransform lt = new LocalTransform()
                {
                        Position = authoring.transform.position,
                        Rotation = authoring.transform.rotation,
                        Scale = 1,
                };
                AddComponent(entity,new ENVObstacleInfo()
                {
                    shape = authoring.shape,
                    radius = authoring.radius,
                    length = authoring.length,
                    width = authoring.width,
                    center = authoring.transform.position,
                    rotation = authoring.transform.rotation,
                    lt = lt,
                });
            }
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = Color;
            switch (shape)
            {
                case ENVObstacleShape.Rectangle:
                    Gizmos.DrawCube(transform.position, new Vector3(length, height, width));
                    break;
                case ENVObstacleShape.Circle:
                    Gizmos.DrawWireSphere(transform.position, radius);
                    break;
                case ENVObstacleShape.Triangle:
                    Gizmos.DrawLine(transform.position, transform.position + new Vector3(length, 0, 0));
                    Gizmos.DrawLine(transform.position, transform.position + new Vector3(0, 0, width));
                    Gizmos.DrawLine(transform.position + new Vector3(length, 0, 0), transform.position + new Vector3(0, 0, width));
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
}