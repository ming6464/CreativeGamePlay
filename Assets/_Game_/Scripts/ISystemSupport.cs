using Unity.Burst;
using Unity.Entities;

namespace _Game_.Scripts
{
    // public interface ISystemSupport : ISystem
    // {
    //     bool IsInitialized { get; set; }
    //
    //     [BurstCompile]
    //     void ISystem.OnCreate(ref SystemState state)
    //     {
    //         RequireNecessaryComponents(ref state);
    //         Init(ref state);
    //         OnCreate(ref state);
    //     }
    //
    //     [BurstCompile]
    //     void ISystem.OnDestroy(ref SystemState state)
    //     {
    //         OnDestroy(ref state);
    //     }
    //
    //     [BurstCompile]
    //     void ISystem.OnUpdate(ref SystemState state)
    //     {
    //         if (!IsInitialized)
    //         {
    //             CheckAndInitRunTime(ref state);
    //             IsInitialized = true;
    //         }
    //
    //         UpdateComponentRunTime(ref state);
    //         OnUpdate(ref state);
    //     }
    //
    //     void RequireNecessaryComponents(ref SystemState state);
    //     void Init(ref SystemState state);
    //     void OnCreate(ref SystemState state);
    //     void OnDestroy(ref SystemState state);
    //     void OnUpdate(ref SystemState state);
    //
    //     void CheckAndInitRunTime(ref SystemState state) { }
    //     void UpdateComponentRunTime(ref SystemState state) { }
    // }
}