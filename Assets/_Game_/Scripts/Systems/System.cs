// using Unity.Burst;
// using Unity.Collections;
// using Unity.Entities;
// using Unity.Mathematics;
// using Unity.Transforms;
//
// namespace _Game_.Scripts.Systems
// {
//     [UpdateInGroup(typeof(PresentationSystemGroup))]
//     public partial struct System : ISystem
//     {
//         private int check;
//         private float time;
//         [BurstCompile]
//         public void OnCreate(ref SystemState state)
//         {
//             time = 5f;
//         }
//
//         [BurstCompile]
//         public void OnUpdate(ref SystemState state)
//         {
//             EntityCommandBuffer ecb = new EntityCommandBuffer(Allocator.Temp);
//             check++;
//             if (check -3 == 0)
//             {
//                 var a = SystemAPI.GetSingleton<ItemProperty>();
//                 for (int i = 0; i < 10; i++)
//                 {
//                     instance(ref ecb, a.entity, i);
//                 }
//                 ecb.Playback(state.EntityManager);
//                 return;
//             }
//             if(time > SystemAPI.Time.ElapsedTime) return;
//             var arr = SystemAPI.QueryBuilder().WithAll<ItemInfo>().Build().ToEntityArray(Allocator.Temp);
//             for (int i = 0; i < arr.Length; i++)
//             {
//                 // if(!state.EntityManager.HasBuffer<Child>(arr[i])) continue;
//                 // var bufferCHild = state.EntityManager.GetBuffer<Child>(arr[i]);
//                 // for (int j = bufferCHild.Length - 1; j >= 0; j--)
//                 // {
//                 //     ecb.DestroyEntity(bufferCHild[j].Value);
//                 // }
//                 // bufferCHild.Clear();
//                 state.EntityManager.DestroyEntity(arr[i]);
//             }
//             ecb.Playback(state.EntityManager);
//             arr.Dispose();
//             ecb.Dispose();
//         }
//
//         private void instance(ref EntityCommandBuffer ecb, Entity entity,int i)
//         {
//             var e = ecb.Instantiate(entity);
//             ecb.AddComponent(e,new LocalTransform()
//             {
//                 Position = new float3(i,0,5),
//                 Rotation = quaternion.identity,
//                 Scale = 1,
//             });
//             ecb.AddComponent<ItemInfo>(e);
//         }
//
//         [BurstCompile]
//         public void OnDestroy(ref SystemState state)
//         {
//
//         }
//     }
// }