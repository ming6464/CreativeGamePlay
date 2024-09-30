using Unity.Entities;
using UnityEngine;

namespace _Game_.Scripts.AuthoringAndMono
{
    public class CountersinkAuthoring : MonoBehaviour
    {
        public float lifeTime;
        public float radius;
        public float height;
        public float damage;
        public float delayTime;
        
        private class CountersinkAuthoringBaker : Baker<CountersinkAuthoring>
        {
            public override void Bake(CountersinkAuthoring authoring)
            {
                Entity entity = GetEntity(TransformUsageFlags.None);
                ItemPropertyEvent itemPropertyEvent = new ItemPropertyEvent()
                {
                    radius = authoring.radius,
                    height = authoring.height,
                    lifeTime = authoring.lifeTime,
                    damage = authoring.damage,
                    delayTime = authoring.delayTime
                };
                AddComponent(entity,new EventPlayOnMono
                {
                    eventTypeOnMono = EventTypeOnMono.CountersinkItem,
                    itemPropertyEvent = itemPropertyEvent,
                });
            }
        }
    }
}