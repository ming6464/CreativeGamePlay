using Unity.Entities;
using UnityEngine;

namespace _Game_.Scripts.ComponentsAndTags.Obstacle
{
    public class TurretAuthoring : MonoBehaviour
    {
        [Tooltip("ID barrel")]
        public int id;
        private class TurretAuthoringBaker : Baker<TurretAuthoring>
        {
            public override void Bake(TurretAuthoring authoring)
            {
                Entity entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent(entity, new BarrelCanSetup()
                {
                    id = authoring.id,
                });
            }
        }
    }

    public struct Config : IComponentData
    {
        public Entity BotPrefab;
        public int    ZombMaxAmount;
    }
}