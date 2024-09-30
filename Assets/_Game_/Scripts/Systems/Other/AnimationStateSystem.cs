using Rukhanka;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;

[UpdateInGroup(typeof(SimulationSystemGroup))]
public partial class AnimationStateSystem : SystemBase
{
    private readonly FastAnimatorParameter _dieAnimatorParameter = new("Die");
    private readonly FastAnimatorParameter _runAnimatorParameter = new("Run");
    private readonly FastAnimatorParameter _attackAnimatorParameter = new("Attack");
    private LayerStoreComponent _layerStoreComponent;
    private bool _isInit;

    protected override void OnCreate()
    {
        base.OnCreate();
        RequireForUpdate<LayerStoreComponent>();
    }

    protected override void OnUpdate()
    {
        CheckAndInit();
        AnimationZombieHandle();
        AnimationPlayerHandle();
    }

    private void CheckAndInit()
    {
        if (!_isInit)
        {
            _isInit = true;
            _layerStoreComponent = SystemAPI.GetSingleton<LayerStoreComponent>();
        }
    }

    private void AnimationPlayerHandle()
    {
        EntityCommandBuffer ecb = new EntityCommandBuffer(Allocator.TempJob);
        var characterAnimJob = new ProcessAnimCharacter()
        {
            runAnimatorParameter = _runAnimatorParameter,
            ecb = ecb.AsParallelWriter(),
            timeDelta = (float)SystemAPI.Time.DeltaTime,
            dieAnimatorParameter = _dieAnimatorParameter,
        };
        Dependency = characterAnimJob.ScheduleParallel(Dependency);
        Dependency.Complete();
        ecb.Playback(EntityManager);
        ecb.Dispose();
    }

    private void AnimationZombieHandle()
    {
        EntityCommandBuffer ecb = new EntityCommandBuffer(Allocator.TempJob);
        var zombieAnimatorJob = new ProcessAnimZombie()
        {
            dieAnimatorParameter = _dieAnimatorParameter,
            timeDelta = (float)SystemAPI.Time.DeltaTime,
            enemyLayer = _layerStoreComponent.enemyLayer,
            enemyDieLayer = _layerStoreComponent.enemyDieLayer,
            attackAnimatorParameter = _attackAnimatorParameter,
            ecb = ecb.AsParallelWriter(),
            runAnimatorParameter = _runAnimatorParameter
        };
        Dependency = zombieAnimatorJob.ScheduleParallel(Dependency);
        Dependency.Complete();
        ecb.Playback(EntityManager);
        ecb.Dispose();
    }


    [BurstCompile]
    partial struct ProcessAnimCharacter : IJobEntity
    {
        public EntityCommandBuffer.ParallelWriter ecb;
        [ReadOnly] public float timeDelta;
        [ReadOnly] public FastAnimatorParameter runAnimatorParameter;
        [ReadOnly] public FastAnimatorParameter dieAnimatorParameter;

        void Execute(in CharacterInfo characterInfo, ref SetAnimationSP setAnimation, Entity entity,
            [EntityIndexInQuery] int indexQuery, AnimatorParametersAspect parametersAspect)
        {
            setAnimation.timeDelay -= timeDelta;
            switch (setAnimation.state)
            {
                case StateID.Enable:
                    parametersAspect.SetBoolParameter(dieAnimatorParameter, false);
                    setAnimation.state = StateID.WaitRemove;
                    break;
                case StateID.None:
                    parametersAspect.SetBoolParameter(runAnimatorParameter, false);
                    setAnimation.state = StateID.WaitRemove;
                    break;
                case StateID.Run:
                    parametersAspect.SetBoolParameter(runAnimatorParameter, true);
                    setAnimation.state = StateID.WaitRemove;
                    break;
                case StateID.Die:
                    parametersAspect.SetBoolParameter(dieAnimatorParameter, true);
                    setAnimation.state = StateID.WaitToPool;
                    break;
                case StateID.WaitToPool:
                    if (setAnimation.timeDelay <= 0)
                    {
                        setAnimation.state = StateID.WaitRemove;
                        ecb.AddComponent(indexQuery, entity, new SetActiveSP()
                        {
                            state = DisableID.DestroyAll,
                        });
                    }

                    break;
                case StateID.Idle:
                    setAnimation.state = StateID.WaitRemove;
                    break;
            }

            if (setAnimation is { state: StateID.WaitRemove, timeDelay: <= 0 })
            {
                ecb.RemoveComponent<SetAnimationSP>(indexQuery, entity);
            }
        }
    }

    [BurstCompile]
    partial struct ProcessAnimZombie : IJobEntity
    {
        public EntityCommandBuffer.ParallelWriter ecb;
        [ReadOnly] public FastAnimatorParameter dieAnimatorParameter;
        [ReadOnly] public FastAnimatorParameter attackAnimatorParameter;
        [ReadOnly] public FastAnimatorParameter runAnimatorParameter;
        [ReadOnly] public float timeDelta;
        [ReadOnly] public uint enemyLayer;
        [ReadOnly] public uint enemyDieLayer;

        void Execute(in ZombieInfo zombieInfo, ref LocalTransform lt, ref ZombieRuntime runtime,
            ref SetAnimationSP setAnimation, Entity entity, [EntityIndexInQuery] int indexQuery,
            AnimatorParametersAspect parametersAspect,
            ref PhysicsCollider physicsCollider)
        {
            setAnimation.timeDelay -= timeDelta;
            var colliderFilter = physicsCollider.Value.Value.GetCollisionFilter();
            runtime.latestAnimState = setAnimation.state;
            switch (setAnimation.state)
            {
                case StateID.Enable:
                    parametersAspect.SetBoolParameter(dieAnimatorParameter, false);
                    parametersAspect.SetBoolParameter(attackAnimatorParameter, false);
                    parametersAspect.SetBoolParameter(runAnimatorParameter, false);
                    colliderFilter.BelongsTo = enemyLayer;
                    physicsCollider.Value.Value.SetCollisionFilter(colliderFilter);
                    setAnimation.state = StateID.WaitRemove;
                    break;
                case StateID.Die:
                    parametersAspect.SetBoolParameter(dieAnimatorParameter, true);
                    setAnimation.state = StateID.WaitToPool;
                    colliderFilter.BelongsTo = enemyDieLayer;
                    physicsCollider.Value.Value.SetCollisionFilter(colliderFilter);
                    break;
                case StateID.WaitToPool:
                    if (setAnimation.timeDelay <= 0)
                    {
                        setAnimation.state = StateID.WaitRemove;
                        ecb.AddComponent(indexQuery, entity, new SetActiveSP()
                        {
                            state = DisableID.Disable,
                        });
                    }
                    else if (setAnimation.timeDelay < 0.2f)
                    {
                        lt.Position = new float3(999, 999, 999);
                    }

                    break;
                case StateID.Attack:
                    parametersAspect.SetBoolParameter(attackAnimatorParameter, true);
                    if (setAnimation.timeDelay <= 0)
                    {
                        setAnimation.state = StateID.WaitRemove;
                        parametersAspect.SetBoolParameter(attackAnimatorParameter, false);
                    }

                    break;
                case StateID.Run:
                    parametersAspect.SetBoolParameter(runAnimatorParameter, true);
                    setAnimation.state = StateID.WaitRemove;
                    break;
                case StateID.Idle:
                    parametersAspect.SetBoolParameter(dieAnimatorParameter, false);
                    parametersAspect.SetBoolParameter(attackAnimatorParameter, false);
                    parametersAspect.SetBoolParameter(runAnimatorParameter, false);
                    setAnimation.state = StateID.WaitRemove;
                    break;
            }

            if (setAnimation is { state: StateID.WaitRemove, timeDelay: <= 0 })
            {
                ecb.RemoveComponent<SetAnimationSP>(indexQuery, entity);
            }
        }
    }
}