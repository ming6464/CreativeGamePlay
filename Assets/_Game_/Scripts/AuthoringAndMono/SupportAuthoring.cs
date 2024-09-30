using System;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace _Game_.Scripts.AuthoringAndMono
{
    public class SupportAuthoring : MonoBehaviour
    {
        [Header("Layer")] 
        public LayerMask defaultLayer;
        public LayerMask playerLayer;
        public LayerMask characterLayer;
        public LayerMask enemyLayer;
        public LayerMask enemyDieLayer;
        public LayerMask bulletLayer;
        public LayerMask itemLayer;
        public LayerMask itemCanShootLayer;
        
        private class SupportAuthoringBaker : Baker<SupportAuthoring>
        {
            public override void Bake(SupportAuthoring authoring)
            {
                Entity entity = GetEntity(TransformUsageFlags.None);
                AddComponent(entity,new LayerStoreComponent()
                {
                    defaultLayer = (uint)authoring.defaultLayer.value,
                    playerLayer = (uint)authoring.playerLayer.value,
                    characterLayer = (uint)authoring.characterLayer.value,
                    enemyLayer = (uint)authoring.enemyLayer.value,
                    enemyDieLayer = (uint)authoring.enemyDieLayer.value,
                    bulletLayer = (uint)authoring.bulletLayer.value,
                    itemLayer = (uint)authoring.itemLayer.value,
                    itemCanShootLayer = (uint)authoring.itemCanShootLayer.value,
                });
                AddBuffer<BufferPolyCastDamageRuntime>(entity);
                AddBuffer<BufferPolyCastDamage>(entity);
            }
        }
    }
}



