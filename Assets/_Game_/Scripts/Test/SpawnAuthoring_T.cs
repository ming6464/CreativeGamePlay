using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

public class SpawnAuthoring_T : MonoBehaviour
{
    public GameObject cube;
    public float2 spawnInfo;
    public bool isUsingBuffer;
    class Spawn_Baker : Baker<SpawnAuthoring_T>
    {
        public override void Bake(SpawnAuthoring_T authoring)
        {
            // var entity = GetEntity(TransformUsageFlags.None);   
            // AddBuffer<BufferSpawn>(entity);
            // AddComponent(entity,new SpawnComponent()
            // {
            //     spawnRange = authoring.spawnInfo,
            //     entity = GetEntity(authoring.cube,TransformUsageFlags.Dynamic),
            //     isUsingBuffer = authoring.isUsingBuffer
            // });
        }
    }
    
}


// public struct BufferSpawn : IBufferElementData
// {
//     public Entity entity;
//     public int x;
//     public int y;
// }

public struct SpawnComponent : IComponentData
{
    public float2 spawnRange;
    public Entity entity;
    public bool isUsingBuffer;
}

public struct CubeComponent : IComponentData
{
    
}

// public partial struct SpawnSystem : ISystem
// {
//     private SpawnComponent _spawnComponent;
//     //
//     private bool _isInit;
//     private bool _isUsingBuffer;
//     private EntityManager _entityManager;
//     public void OnCreate(ref SystemState state)
//     {
//         state.RequireForUpdate<SpawnComponent>();
//     }
//
//     public void OnUpdate(ref SystemState state)
//     {
//         _entityManager = state.EntityManager;
//         DynamicBuffer<BufferSpawn> buffer = default;
//         if (!_isInit)
//         {
//             _isInit = true;
//             //
//             _spawnComponent = SystemAPI.GetSingleton<SpawnComponent>();
//             if (_spawnComponent.isUsingBuffer)
//             {
//                 _isUsingBuffer = true;
//                 buffer = SystemAPI.GetSingletonBuffer<BufferSpawn>();
//             }
//             var range = _spawnComponent.spawnRange;
//             for(int i = 0; i < range.x; i++)
//             {
//                 for (int j = 0; j < range.y; j++)
//                 {
//                     var cube = _entityManager.Instantiate(_spawnComponent.entity);
//                     _entityManager.AddComponent<CubeComponent>(cube);
//                     _entityManager.AddComponentData(cube, new LocalTransform()
//                     {
//                         Position = new float3(i, 0, j),
//                         Rotation = quaternion.identity,
//                         Scale = 1,
//                     });
//
//                     if (_spawnComponent.isUsingBuffer)
//                     {
//                         buffer.Add(new BufferSpawn()
//                         {
//                             entity = cube,
//                             x = i,
//                             y = j,
//                         });
//                     }
//                     
//                 }
//             }
//             return;
//         }
//
//         float time = (float) SystemAPI.Time.ElapsedTime;
//
//         var lt = new LocalTransform()
//         {
//             Scale = 1,
//             Rotation = quaternion.identity
//         };
//         
//         if (_isUsingBuffer)
//         {
//             var entity = SystemAPI.GetSingletonEntity<SpawnComponent>();
//             buffer = _entityManager.GetBuffer<BufferSpawn>(entity);
//             foreach (var b in buffer)
//             {
//                 lt.Position = new float3(b.x, math.sin((b.x + b.y) * time), b.y);
//                 _entityManager.SetComponentData(b.entity,lt);
//             }
//         }
//         else
//         {
//             foreach (var (localTr, entity) in SystemAPI.Query<RefRW<LocalTransform>>().WithEntityAccess()
//                          .WithAll<CubeComponent>())
//             {
//                 var b = localTr.ValueRW.Position;
//                 localTr.ValueRW.Position = new float3(b.x, math.sin((b.x + b.y) * time), b.y);
//             }
//         }
//         
//     }
// }
