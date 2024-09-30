using Rukhanka;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;
using UnityEngine;
using Random = Unity.Mathematics.Random;

[UpdateInGroup(typeof(SimulationSystemGroup)), UpdateBefore(typeof(AnimationStateSystem))]
[BurstCompile]
public partial struct ZombieSystem : ISystem
{
    private bool                                 _init;
    private float3                               _pointZoneMin;
    private float3                               _pointZoneMax;
    private Entity                               _entityPlayerInfo;
    private EntityTypeHandle                     _entityTypeHandle;
    private EntityManager                        _entityManager;
    private NativeQueue<TakeDamageItem>          _takeDamageQueue;
    private NativeList<CharacterSetTakeDamage>   _characterSetTakeDamages;
    private NativeList<float3>                   _characterLtws;
    private ComponentTypeHandle<LocalToWorld>    _ltwTypeHandle;
    private ComponentTypeHandle<ZombieRuntime>   _zombieRunTimeTypeHandle;
    private ComponentTypeHandle<ZombieInfo>      _zombieInfoTypeHandle;
    private ComponentTypeHandle<ZombieAvoidData> _zombieAvoidDataTypeHandle;
    private ComponentTypeHandle<LocalTransform>  _ltTypeHandle;
    private ComponentTypeHandle<ZombieState>  _zombieStateTypeHandle;
    private LayerStoreComponent                  _layerStore;
    private CollisionFilter                      _collisionFilter;
    private PhysicsWorldSingleton                _physicsWorld;
    private ZombieProperty                       _zombieProperty;
    private NativeArray<ENVObstacleInfo>         _envObstacleInfos;

    // Query
    private EntityQuery _enQueryZombie;
    private EntityQuery _enQueryZombieNormal;
    private EntityQuery _enQueryZombieBoss;
    private EntityQuery _enQueryZombieNew;

    // HASH
    private uint _attackHash;
    private uint _finishAttackHash;

    private uint _groundCrackEffectHash;

    //
    private int _avoidFrameCheck;

    #region CREATE

    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        RequireNecessaryComponents(ref state);
        Init(ref state);
    }

    [BurstCompile]
    private void Init(ref SystemState state)
    {
        _ltwTypeHandle             = state.GetComponentTypeHandle<LocalToWorld>();
        _zombieRunTimeTypeHandle   = state.GetComponentTypeHandle<ZombieRuntime>();
        _zombieInfoTypeHandle      = state.GetComponentTypeHandle<ZombieInfo>();
        _ltTypeHandle              = state.GetComponentTypeHandle<LocalTransform>();
        _zombieStateTypeHandle     = state.GetComponentTypeHandle<ZombieState>();
        _zombieAvoidDataTypeHandle = state.GetComponentTypeHandle<ZombieAvoidData>();
        _enQueryZombieNew = SystemAPI.QueryBuilder().WithAll<ZombieInfo, New>().WithNone<Disabled, AddToBuffer>()
                                     .Build();
        _enQueryZombie =
                SystemAPI.QueryBuilder().WithAll<ZombieInfo, LocalTransform>()
                         .WithNone<Disabled, AddToBuffer, New, SetAnimationSP>().Build();
        _enQueryZombieNormal =
                SystemAPI.QueryBuilder().WithAll<ZombieInfo, LocalTransform>()
                         .WithNone<Disabled, AddToBuffer, New, SetAnimationSP, BossInfo>().Build();
        _enQueryZombieBoss =
                SystemAPI.QueryBuilder().WithAll<ZombieInfo, LocalTransform>()
                         .WithNone<Disabled, AddToBuffer, New, SetAnimationSP>().Build();
        _takeDamageQueue         = new NativeQueue<TakeDamageItem>(Allocator.Persistent);
        _characterSetTakeDamages = new NativeList<CharacterSetTakeDamage>(Allocator.Persistent);
        _characterLtws           = new NativeList<float3>(Allocator.Persistent);
        _attackHash              = FixedStringExtensions.CalculateHash32("Attack");
        _finishAttackHash        = FixedStringExtensions.CalculateHash32("FinishAttack");
        _groundCrackEffectHash   = FixedStringExtensions.CalculateHash32("GroundCrack");
    }

    [BurstCompile]
    private void RequireNecessaryComponents(ref SystemState state)
    {
        state.RequireForUpdate<LayerStoreComponent>();
        state.RequireForUpdate<PhysicsWorldSingleton>();
        state.RequireForUpdate<PlayerInfo>();
        state.RequireForUpdate<ZombieProperty>();
        state.RequireForUpdate<ActiveZoneProperty>();
        state.RequireForUpdate<ZombieInfo>();
        state.RequireForUpdate<ENVObstacleInfo>();
    }
    #endregion
    
    [BurstCompile]
    public void OnDestroy(ref SystemState state)
    {
        if (_characterSetTakeDamages.IsCreated)
            _characterSetTakeDamages.Dispose();
        if (_takeDamageQueue.IsCreated)
            _takeDamageQueue.Dispose();
        if (_characterLtws.IsCreated)
            _characterLtws.Dispose();
        if (_envObstacleInfos.IsCreated)
            _envObstacleInfos.Dispose();
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        if (!CheckAndInit(ref state))
            return;
        SetUpNewZombie(ref state);
        Move(ref state);
        CheckAttackPlayer(ref state);
        CheckDeadZone(ref state);
        CheckAnimationEvent(ref state);
    }

    private void UpdateTypeHandle(ref SystemState state)
    {
        _ltwTypeHandle.Update(ref state);
        _zombieRunTimeTypeHandle.Update(ref state);
        _zombieInfoTypeHandle.Update(ref state);
        _ltTypeHandle.Update(ref state);
        _zombieAvoidDataTypeHandle.Update(ref state);
        _entityTypeHandle.Update(ref state);
        _zombieStateTypeHandle.Update(ref state);
    }

    private void UpdateObstacle(ref SystemState state)
    {
        if (_envObstacleInfos.IsCreated)
        {
            _envObstacleInfos.Dispose();
        }

        var envObsacleInfos = new NativeList<ENVObstacleInfo>(Allocator.Temp);

        foreach (var (envObsacleInfo, entity) in SystemAPI.Query<RefRO<ENVObstacleInfo>>().WithEntityAccess())
        {
            var envObstacleInfo = envObsacleInfo.ValueRO;
            envObsacleInfos.Add(envObstacleInfo);
        }

        _envObstacleInfos = envObsacleInfos.ToArray(Allocator.Persistent);
        envObsacleInfos.Dispose();
    }

    [BurstCompile]
    private void SetUpNewZombie(ref SystemState state)
    {
        UpdateTypeHandle(ref state);
        var job = new SetUpNewZombieJOB()
        {
                ltTypeHandle         = _ltTypeHandle,
                zombieInfoTypeHandle = _zombieInfoTypeHandle,
        };
        state.Dependency = job.ScheduleParallel(_enQueryZombieNew, state.Dependency);
        state.Dependency.Complete();
    }

    [BurstCompile]
    private bool CheckAndInit(ref SystemState state)
    {
        if (_init)
            return true;
        _init = true;
        var zone = SystemAPI.GetSingleton<ActiveZoneProperty>();
        _pointZoneMin     = zone.pointRangeMin;
        _pointZoneMax     = zone.pointRangeMax;
        _entityPlayerInfo = SystemAPI.GetSingletonEntity<PlayerInfo>();
        _entityManager    = state.EntityManager;
        _layerStore       = SystemAPI.GetSingleton<LayerStoreComponent>();
        _collisionFilter = new CollisionFilter()
        {
                BelongsTo    = _layerStore.enemyLayer,
                CollidesWith = _layerStore.characterLayer,
                GroupIndex   = 0
        };
        _zombieProperty = SystemAPI.GetSingleton<ZombieProperty>();

        return false;
    }

    #region Animation Event

    [BurstCompile]
    private void CheckAnimationEvent(ref SystemState state)
    {
        _physicsWorld = SystemAPI.GetSingleton<PhysicsWorldSingleton>();
        EntityCommandBuffer ecb = new EntityCommandBuffer(Allocator.TempJob);

        var query = SystemAPI.Query<RefRO<ZombieInfo>>()
                             .WithEntityAccess().WithNone<Disabled, AddToBuffer>();
        
        // Check event boss
        foreach (var (zombieInfo, entity) in query)
        {
            if (_entityManager.HasBuffer<AnimationEventComponent>(entity))
            {
                HandleEvent(ref state, ref ecb, entity, zombieInfo.ValueRO);
            }
        }

        ecb.Playback(_entityManager);
        ecb.Dispose();
    }

    [BurstCompile]
    private void HandleEvent(ref SystemState state, ref EntityCommandBuffer ecb, Entity entity, ZombieInfo zombieInfo)
    {
        foreach (var b in _entityManager.GetBuffer<AnimationEventComponent>(entity))
        {
            var funcName = b.nameHash;

            if (funcName.CompareTo(_attackHash) == 0)
            {
                Debug.Log("m _ Attack Name hash");
                var entityNew      = _entityManager.CreateEntity();
                var lt             = _entityManager.GetComponentData<LocalToWorld>(entity);
                var attackPosition = math.transform(lt.Value, zombieInfo.offsetAttackPosition);

                if (b.intParam >= 0)
                {
                    var eff = new EffectPropertyEvent()
                    {
                            position = attackPosition,
                            rotation = lt.Rotation,
                            effectID = (EffectID)b.intParam
                    };

                    var eventMono = new EventPlayOnMono()
                    {
                            eventTypeOnMono     = EventTypeOnMono.Effect,
                            effectPropertyEvent = eff,
                    };

                    ecb.AddComponent(entityNew, eventMono);
                }

                NativeList<ColliderCastHit> hits = new NativeList<ColliderCastHit>(Allocator.Temp);

                if (_physicsWorld.SphereCastAll(attackPosition, zombieInfo.radiusDamage, float3.zero, 0, ref hits,
                            _collisionFilter))
                {
                    foreach (var hit in hits)
                    {
                        var takeDamage = new TakeDamage();

                        if (_entityManager.HasComponent<TakeDamage>(hit.Entity))
                        {
                            takeDamage = _entityManager.GetComponentData<TakeDamage>(hit.Entity);
                        }

                        takeDamage.value += zombieInfo.damage;
                        ecb.AddComponent(hit.Entity, takeDamage);
                    }
                }

                hits.Dispose();
            }
            else if (funcName.CompareTo(_finishAttackHash) == 0)
            {
                ecb.AddComponent(entity, new SetAnimationSP()
                {
                        state     = StateID.Idle,
                        timeDelay = 0,
                });
            }
        }
    }

    #endregion

    #region Attack Character

    [BurstCompile]
    private void CheckAttackPlayer(ref SystemState state)
    {
        _takeDamageQueue.Clear();
        _characterSetTakeDamages.Clear();

        foreach (var (ltw, entity) in SystemAPI.Query<RefRO<LocalToWorld>>().WithEntityAccess().WithAll<CharacterInfo>()
                                               .WithNone<Disabled, AddToBuffer, SetActiveSP>())
        {
            _characterSetTakeDamages.Add(new()
            {
                    entity   = entity,
                    position = ltw.ValueRO.Position
            });
        }

        if (_characterSetTakeDamages.Length == 0)
            return;
        var ecb            = new EntityCommandBuffer(Allocator.TempJob);
        var playerPosition = SystemAPI.GetComponentRO<LocalToWorld>(_entityPlayerInfo).ValueRO.Position;

        ZombieAttack(ref state,ref ecb, ref _takeDamageQueue, playerPosition);

        HandleAttackedCharacter(ref state, ref ecb);

        ecb.Playback(_entityManager);
        ecb.Dispose();
    }

    [BurstCompile]
    private void HandleAttackedCharacter(ref SystemState state, ref EntityCommandBuffer ecb)
    {
        var characterTakeDamageMap =
                new NativeHashMap<int, float>(_takeDamageQueue.Count, Allocator.Temp);

        while (_takeDamageQueue.TryDequeue(out var queue))
        {
            if (characterTakeDamageMap.ContainsKey(queue.index))
            {
                characterTakeDamageMap[queue.index] += queue.damage;
            }
            else
            {
                characterTakeDamageMap.Add(queue.index, queue.damage);
            }
        }

        foreach (var map in characterTakeDamageMap)
        {
            if (map.Value == 0)
                continue;
            var entity = _characterSetTakeDamages[map.Key].entity;
            ecb.AddComponent(entity, new TakeDamage()
            {
                    value = map.Value,
            });
        }

        characterTakeDamageMap.Dispose();
    }

    [BurstCompile]
    private void ZombieAttack(ref SystemState state,ref EntityCommandBuffer ecb, ref NativeQueue<TakeDamageItem> takeDamageQueue,
                              float3          playerPosition)
    {
        UpdateTypeHandle(ref state);
        var jobBoss = new CheckZombieBossAttackPlayerJOB()
        {
                characterSetTakeDamages = _characterSetTakeDamages,
                ecb                     = ecb.AsParallelWriter(),
                entityTypeHandle        = _entityTypeHandle,
                localToWorldTypeHandle  = _ltwTypeHandle,
                time                    = (float)SystemAPI.Time.ElapsedTime,
                timeDelay               = 2.2f,
                zombieInfoTypeHandle    = _zombieInfoTypeHandle,
                zombieRuntimeTypeHandle = _zombieRunTimeTypeHandle,
                zombieStateTypeHandle   = _zombieStateTypeHandle,
        };
        state.Dependency = jobBoss.ScheduleParallel(_enQueryZombieBoss, state.Dependency);
        state.Dependency.Complete();
        
        // UpdateTypeHandle(ref state);
        // var job = new CheckAttackPlayerJOB()
        // {
        //         characterSetTakeDamages = _characterSetTakeDamages,
        //         localToWorldTypeHandle  = _ltwTypeHandle,
        //         time                    = (float)SystemAPI.Time.ElapsedTime,
        //         zombieInfoTypeHandle    = _zombieInfoTypeHandle,
        //         zombieRuntimeTypeHandle = _zombieRunTimeTypeHandle,
        //         takeDamageQueues        = takeDamageQueue.AsParallelWriter(),
        //         playerPosition          = playerPosition,
        //         distanceCheck           = 10,
        // };
        //
        // state.Dependency = job.ScheduleParallel(_enQueryZombieNormal, state.Dependency);
        // state.Dependency.Complete();
    }
    

    #endregion

    #region MOVE

    [BurstCompile]
    private void Move(ref SystemState state)
    {
        AvoidFlock(ref state);
        UpdateCharacterLTWList(ref state);
        float               deltaTime = SystemAPI.Time.DeltaTime;
        EntityCommandBuffer ecb       = new EntityCommandBuffer(Allocator.TempJob);
        UpdateTypeHandle(ref state);
        ZombieMovementJOB job = new ZombieMovementJOB()
        {
                deltaTime                 = deltaTime,
                ltTypeHandle              = _ltTypeHandle,
                zombieInfoTypeHandle      = _zombieInfoTypeHandle,
                characterLtws             = _characterLtws,
                entityTypeHandle          = _entityTypeHandle,
                zombieRunTimeTypeHandle   = _zombieRunTimeTypeHandle,
                zombieAvoidDataTypeHandle = _zombieAvoidDataTypeHandle,
                ecb                       = ecb.AsParallelWriter(),
        };
        state.Dependency = job.ScheduleParallel(_enQueryZombie, state.Dependency);
        state.Dependency.Complete();
        ecb.Playback(_entityManager);
        ecb.Dispose();
    }


    [BurstCompile]
    private void AvoidFlock(ref SystemState state)
    {
        // _avoidFrameCheck++;
        //
        // if (_avoidFrameCheck >= 2)
        // {
        //     _avoidFrameCheck -= 2;
        // }

        if (!_zombieProperty.comparePriorities || _avoidFrameCheck != 0)
            return;
        UpdateObstacle(ref state);
        UpdatePriority(ref state);

        var avoidDatas = new NativeList<AvoidData>(Allocator.TempJob);

        foreach (var (ìnfo, lt, entity) in SystemAPI.Query<RefRO<ZombieInfo>, RefRO<LocalTransform>>()
                                                    .WithEntityAccess().WithNone<Disabled, SetActiveSP, AddToBuffer>())
        {
            avoidDatas.Add(new()
            {
                    entity = entity,
                    info   = ìnfo.ValueRO,
                    lt     = lt.ValueRO,
            });
        }
        UpdateTypeHandle(ref state);
        EntityCommandBuffer ecb = new EntityCommandBuffer(Allocator.TempJob);
        var job = new ZombieAvoidJOB()
        {
                avoidDatas     = avoidDatas,
                zombieProperty = _zombieProperty,
                deltaTime      = SystemAPI.Time.DeltaTime,
                ecb            = ecb.AsParallelWriter(),
        };

        state.Dependency = job.Schedule(avoidDatas.Length, (int)avoidDatas.Length / 40);
        state.Dependency.Complete();
        ecb.Playback(_entityManager);
        ecb.Dispose();
        avoidDatas.Dispose();
    }

    [BurstCompile]
    private void UpdatePriority(ref SystemState state)
    {
        UpdateTypeHandle(ref state);
        var playerPosition = SystemAPI.GetComponentRO<LocalToWorld>(_entityPlayerInfo).ValueRO.Position;
        var job = new UpdatePriorityJOB()
        {
                ltTypeHandle              = _ltTypeHandle,
                zombieInfoTypeHandle      = _zombieInfoTypeHandle,
                playerPos                 = playerPosition,
                envObstacleInfos          = _envObstacleInfos,
                zombieAvoidDataTypeHandle = _zombieAvoidDataTypeHandle
        };

        state.Dependency = job.ScheduleParallel(_enQueryZombie, state.Dependency);
        state.Dependency.Complete();
    }

    [BurstCompile]
    private void UpdateCharacterLTWList(ref SystemState state)
    {
        _characterLtws.Clear();

        foreach (var ltw in SystemAPI.Query<RefRO<LocalToWorld>>().WithAll<CharacterInfo>()
                                     .WithNone<Disabled, SetActiveSP, AddToBuffer>())
        {
            _characterLtws.Add(ltw.ValueRO.Position);
        }
    }

    #endregion

    [BurstCompile]
    private void CheckDeadZone(ref SystemState state)
    {
        var ecb = new EntityCommandBuffer(Allocator.TempJob);
        UpdateTypeHandle(ref state);
        var chunkJob = new CheckDeadZoneJOB
        {
                ecb                  = ecb.AsParallelWriter(),
                entityTypeHandle     = _entityTypeHandle,
                ltwTypeHandle        = _ltwTypeHandle,
                zombieInfoTypeHandle = _zombieInfoTypeHandle,
                minPointRange        = _pointZoneMin,
                maxPointRange        = _pointZoneMax,
        };
        state.Dependency = chunkJob.ScheduleParallel(_enQueryZombie, state.Dependency);
        state.Dependency.Complete();
        ecb.Playback(_entityManager);
        ecb.Dispose();
    }

    #region JOB

    [BurstCompile]
    partial struct ZombieMovementJOB : IJobChunk
    {
        public ComponentTypeHandle<LocalTransform> ltTypeHandle;
        public EntityCommandBuffer.ParallelWriter  ecb;
        public EntityTypeHandle                    entityTypeHandle;
        public ComponentTypeHandle<ZombieInfo>     zombieInfoTypeHandle;

        [ReadOnly]
        public ComponentTypeHandle<ZombieAvoidData> zombieAvoidDataTypeHandle;

        [ReadOnly]
        public ComponentTypeHandle<ZombieRuntime> zombieRunTimeTypeHandle;

        [ReadOnly]
        public float deltaTime;

        [ReadOnly]
        public NativeList<float3> characterLtws;


        public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask,
                            in v128           chunkEnabledMask)
        {
            
            var lts            = chunk.GetNativeArray(ref ltTypeHandle);
            var zombieAvoids   = chunk.GetNativeArray(ref zombieAvoidDataTypeHandle);
            var zombieRunTimes = chunk.GetNativeArray(ref zombieRunTimeTypeHandle);
            var zombieInfos    = chunk.GetNativeArray(ref zombieInfoTypeHandle);
            var entities       = chunk.GetNativeArray(entityTypeHandle);

            for (var i = 0; i < chunk.Count; i++)
            {
                var lt          = lts[i];
                var zombieAvoid = zombieAvoids[i];
                var info        = zombieInfos[i];
                var direct      = GetDirect(lt.Position, info.directNormal, info.chasingRange, info.attackRange);
                var dot         = math.dot(math.normalize(direct), math.normalize(zombieAvoid.avoidDirection));

                if (math.isnan(dot))
                {
                    dot = 1;
                }
                dot = math.remap(-1, 1, 0, 1, dot);
                lt.Rotation = MathExt.MoveTowards(lt.Rotation, quaternion.LookRotationSafe(direct, math.up()),
                        250 * deltaTime);

                lt                 = lt.Translate(direct * dot * info.speed * deltaTime);
                info.currentDirect = direct;
                lts[i]             = lt;
                zombieInfos[i]     = info;

                if (zombieRunTimes[i].latestAnimState != StateID.Run)
                {
                    ecb.AddComponent(unfilteredChunkIndex, entities[i], new SetAnimationSP()
                    {
                            state = StateID.Run,
                    });
                }
            }
        }

        private float3 GetDirect(float3 position, float3 defaultDirect, float chasingRange, float attackRange)
        {
            float3 nearestPosition = default;
            float  distanceNearest = float.MaxValue;

            foreach (var characterLtw in characterLtws)
            {
                float distance = math.distance(characterLtw, position);

                if (distance <= chasingRange && distance < distanceNearest)
                {
                    distanceNearest = distance;
                    nearestPosition = characterLtw;
                }
            }

            if (distanceNearest < float.MaxValue)
            {
                if (math.all(position == nearestPosition))
                {
                    return float3.zero;
                }

                var normalDir = math.normalize(nearestPosition - position);

                if (distanceNearest <= (attackRange / 2f))
                {
                    normalDir *= 0.01f;
                }

                return normalDir;
            }

            return defaultDirect;
        }
    }


    [BurstCompile]
    partial struct ZombieAvoidJOB : IJobParallelFor
    {
        public EntityCommandBuffer.ParallelWriter ecb;

        [ReadOnly]
        public ZombieProperty zombieProperty;

        [ReadOnly]
        public NativeList<AvoidData> avoidDatas;

        [ReadOnly]
        public float deltaTime;

        public void Execute(int index)
        {
            float3 directPushed = float3.zero;
            var    avoidData    = avoidDatas[index];
            var    countAvoid   = 0;

            for (var i = 0; i < avoidDatas.Length; i++)
            {
                if (i == index)
                    continue;
            
                var dataSet = avoidDatas[i];
            
                if (dataSet.info.priority > avoidData.info.priority)
                    continue;
            
                var distance            = math.distance(dataSet.lt.Position, avoidData.lt.Position);
                var overlappingDistance = (dataSet.info.radius + avoidData.info.radius) - distance;
            
                if (overlappingDistance > 0 || math.abs(overlappingDistance) < zombieProperty.minDistanceAvoid)
                {
                    var direct = avoidData.lt.Position - dataSet.lt.Position;
                
                    if (direct.Equals(float3.zero))
                    {
                        var random = new Random((uint)(i * 1.5f));
                        direct = new float3(random.NextFloat(-0.1f, 0.1f), 0,
                                random.NextFloat(-0.1f, 0.1f));
                    }
                
                    var length = math.length(direct);
                    length = math.clamp(math.remap(zombieProperty.minDistanceAvoid, 0, 1, 3, length), 1, 3);
                
                    directPushed += math.normalize(direct) * length;
                    countAvoid++;
                }
            }

            if (directPushed.Equals(float3.zero))
            {
                ecb.SetComponent(index, avoidData.entity,
                        new ZombieAvoidData { avoidDirection = avoidData.info.directNormal });
            }
            else
            {
                directPushed   *= countAvoid;
                directPushed.y =  0;
                var lt = avoidData.lt;
                lt = lt.Translate(directPushed * deltaTime * zombieProperty.speedAvoid);
                ecb.SetComponent(index, avoidData.entity, lt);
                ecb.SetComponent(index, avoidData.entity, new ZombieAvoidData { avoidDirection = directPushed });
            }
        }
    }

    [BurstCompile]
    partial struct UpdatePriorityJOB : IJobChunk
    {
        [ReadOnly]
        public float3 playerPos;

        [ReadOnly]
        public ComponentTypeHandle<ZombieAvoidData> zombieAvoidDataTypeHandle;

        public ComponentTypeHandle<ZombieInfo> zombieInfoTypeHandle;

        [ReadOnly]
        public ComponentTypeHandle<LocalTransform> ltTypeHandle;

        [ReadOnly]
        public NativeArray<ENVObstacleInfo> envObstacleInfos;

        public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask,
                            in v128           chunkEnabledMask)
        {
            var lts        = chunk.GetNativeArray(ref ltTypeHandle);
            var infos      = chunk.GetNativeArray(ref zombieInfoTypeHandle);
            var avoidDatas = chunk.GetNativeArray(ref zombieAvoidDataTypeHandle);

            for (var i = 0; i < chunk.Count; i++)
            {
                var info      = infos[i];
                var lt        = lts[i];
                var avoidData = avoidDatas[i];

                var distance = math.distance(lt.Position, playerPos);

                if (distance > info.chasingRange)
                {
                    distance = math.distance(lt.Position, info.destination);
                }

                float3 pointForce    = float3.zero;
                float  distanceToENV = 0;
                float  ratio         = RatioPriority(lt.Position, info.radius, ref pointForce);
                float  priority      = (distance * 1000) * ratio;
                priority                    += (int)info.priorityKey * 10000;
                info.priority               =  (int)priority;
                avoidData.avoidDirectionENV =  (lt.Position - pointForce) * (1 - ratio);
                infos[i]                    =  info;
            }
        }

        private float RatioPriority(float3 position, float radius, ref float3 pointForce)
        {
            var distance = 99f;
            position.y = 0;

            foreach (var env in envObstacleInfos)
            {
                var pointF = float3.zero;
                var d      = GetDistanceToRectangle(env.lt, env.length, env.width, position, ref pointF) - radius;

                if (d >= distance)
                {
                    continue;
                }

                pointForce = pointF;
                distance   = d;
            }

            return math.min(math.remap(0.5f, 3, 0, 1, distance), 1);
        }

        private float GetDistanceToRectangle(LocalTransform rectangle, float width, float height, float3 point,
                                             ref float3     pointForce)
        {
            point.y = 0;
            var    x                           = width / 2.0f;
            var    y                           = height / 2.0f;
            float3 pointInvertToLocalRectangle = rectangle.InverseTransformPoint(point);
            pointForce = rectangle.TransformPoint(new float3(math.clamp(pointInvertToLocalRectangle.x, -x, x), 0,
                    math.clamp(pointInvertToLocalRectangle.z, -y, y)));
            //----------------
            float3 absPoint = new float3(math.abs(pointInvertToLocalRectangle.x),
                    math.abs(pointInvertToLocalRectangle.y), math.abs(pointInvertToLocalRectangle.z));

            return math.sqrt(math.pow(math.max(absPoint.x - x, 0), 2) + math.pow(math.max(absPoint.z - y, 0), 2));
        }
    }


    [BurstCompile]
    partial struct CheckDeadZoneJOB : IJobChunk
    {
        public EntityCommandBuffer.ParallelWriter ecb;

        [ReadOnly]
        public EntityTypeHandle entityTypeHandle;

        [ReadOnly]
        public ComponentTypeHandle<LocalToWorld> ltwTypeHandle;

        public ComponentTypeHandle<ZombieInfo> zombieInfoTypeHandle;

        [ReadOnly]
        public float3 minPointRange;

        [ReadOnly]
        public float3 maxPointRange;

        public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask,
                            in v128           chunkEnabledMask)
        {
            var ltwArr      = chunk.GetNativeArray(ref ltwTypeHandle);
            var zombieInfos = chunk.GetNativeArray(ref zombieInfoTypeHandle);
            var entities    = chunk.GetNativeArray(entityTypeHandle);

            for (int i = 0; i < chunk.Count; i++)
            {
                if (CheckInRange_L(ltwArr[i].Position, minPointRange, maxPointRange))
                    continue;
                var zombieInfo = zombieInfos[i];
                zombieInfo.hp  = 0;
                zombieInfos[i] = zombieInfo;
                ecb.AddComponent(unfilteredChunkIndex, entities[i], new SetActiveSP
                {
                        state = DisableID.Disable,
                });

                ecb.AddComponent(unfilteredChunkIndex, entities[i], new AddToBuffer()
                {
                        id     = zombieInfo.id,
                        entity = entities[i],
                });
            }

            bool CheckInRange_L(float3 value, float3 min, float3 max)
            {
                if ((value.x - min.x) * (max.x - value.x) < 0)
                    return false;
                if ((value.y - min.y) * (max.y - value.y) < 0)
                    return false;
                if ((value.z - min.z) * (max.z - value.z) < 0)
                    return false;

                return true;
            }
        }
    }

    [BurstCompile]
    partial struct CheckAttackPlayerJOB : IJobChunk
    {
        public ComponentTypeHandle<ZombieRuntime> zombieRuntimeTypeHandle;

        [WriteOnly]
        public NativeQueue<TakeDamageItem>.ParallelWriter takeDamageQueues;

        [ReadOnly]
        public ComponentTypeHandle<ZombieInfo> zombieInfoTypeHandle;

        [ReadOnly]
        public ComponentTypeHandle<LocalToWorld> localToWorldTypeHandle;

        [ReadOnly]
        public NativeList<CharacterSetTakeDamage> characterSetTakeDamages;

        [ReadOnly]
        public float time;

        [ReadOnly]
        public float3 playerPosition;

        [ReadOnly]
        public float distanceCheck;

        public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask,
                            in v128           chunkEnabledMask)
        {
            var zombieInfos    = chunk.GetNativeArray(ref zombieInfoTypeHandle);
            var ltws           = chunk.GetNativeArray(ref localToWorldTypeHandle);
            var zombieRuntimes = chunk.GetNativeArray(ref zombieRuntimeTypeHandle);

            for (int i = 0; i < chunk.Count; i++)
            {
                var info    = zombieInfos[i];
                var ltw     = ltws[i];
                var runtime = zombieRuntimes[i];

                if (time - runtime.latestTimeAttack < info.delayAttack)
                    continue;

                if (math.distance(playerPosition, ltw.Position) > distanceCheck)
                    continue;

                bool checkAttack = false;

                for (int j = 0; j < characterSetTakeDamages.Length; j++)
                {
                    var character = characterSetTakeDamages[j];

                    if (math.distance(character.position, ltw.Position) <= info.attackRange)
                    {
                        takeDamageQueues.Enqueue(new TakeDamageItem()
                        {
                                index  = j,
                                damage = info.damage,
                        });
                        checkAttack = true;
                    }
                }

                if (checkAttack)
                {
                    runtime.latestTimeAttack = time;
                    zombieRuntimes[i]        = runtime;
                }
            }
        }
    }

    [BurstCompile]
    partial struct SetUpNewZombieJOB : IJobChunk
    {
        public ComponentTypeHandle<LocalTransform> ltTypeHandle;

        [ReadOnly]
        public ComponentTypeHandle<ZombieInfo> zombieInfoTypeHandle;

        public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask,
                            in v128           chunkEnabledMask)
        {
            var lts         = chunk.GetNativeArray(ref ltTypeHandle);
            var zombieInfos = chunk.GetNativeArray(ref zombieInfoTypeHandle);

            for (int i = 0; i < chunk.Count; i++)
            {
                var info = zombieInfos[i];
                var lt   = lts[i];

                lt.Rotation = quaternion.LookRotationSafe(info.directNormal, math.up());
                lts[i]      = lt;
            }
        }
    }


    [BurstCompile]
    partial struct CheckZombieBossAttackPlayerJOB : IJobChunk
    {
        public ComponentTypeHandle<ZombieRuntime> zombieRuntimeTypeHandle;
        public EntityCommandBuffer.ParallelWriter ecb;
        public EntityTypeHandle                   entityTypeHandle;

        [ReadOnly]
        public NativeList<CharacterSetTakeDamage> characterSetTakeDamages;

        [ReadOnly]
        public ComponentTypeHandle<ZombieInfo> zombieInfoTypeHandle;

        [ReadOnly]
        public ComponentTypeHandle<LocalToWorld> localToWorldTypeHandle;
        
        public ComponentTypeHandle<ZombieState> zombieStateTypeHandle;

        [ReadOnly]
        public float timeDelay;

        [ReadOnly]
        public float time;

        public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask,
                            in v128           chunkEnabledMask)
        {
            var zombieInfos    = chunk.GetNativeArray(ref zombieInfoTypeHandle);
            var ltws           = chunk.GetNativeArray(ref localToWorldTypeHandle);
            var zombieRuntimes = chunk.GetNativeArray(ref zombieRuntimeTypeHandle);
            var entities       = chunk.GetNativeArray(entityTypeHandle);
            var zombieStates   = chunk.GetNativeArray(zombieStateTypeHandle);

            for (int i = 0; i < chunk.Count; i++)
            {
                var info                  = zombieInfos[i];
                var ltw                   = ltws[i];
                var runtime               = zombieRuntimes[i];
                var state                 = zombieStates[i];
                var indexCharacterNearest = GetIndexCharacterNearest(ltw.Position);

                if (indexCharacterNearest < 0)
                    continue;
                var characterNearest = characterSetTakeDamages[indexCharacterNearest];
                var distance         = info.attackRange - math.distance(ltw.Position, characterNearest.position);

                if (!(distance >= 0))
                {
                    continue;
                }

                var timeCheck = time - runtime.latestTimeAttack;

                if (timeCheck < info.delayAttack)
                {
                    if (runtime.latestAnimState != StateID.Idle)
                    {
                        ecb.AddComponent(unfilteredChunkIndex, entities[i], new SetAnimationSP()
                        {
                                state     = StateID.Idle,
                                timeDelay = info.delayAttack - timeCheck,
                        });
                        state.isAttack = false;
                    }

                    return;
                }

                if (MathExt.CalculateAngle(ltw.Forward, characterNearest.position - ltw.Position) > 45)
                {
                    return;
                }

                ecb.AddComponent(unfilteredChunkIndex, entities[i], new SetAnimationSP()
                {
                        state     = StateID.Attack,
                        timeDelay = 999,
                });
                    
                state.isAttack           = true;
                runtime.latestTimeAttack = time + timeDelay;
                zombieRuntimes[i]        = runtime;

                break;
            }
        }

        private int GetIndexCharacterNearest(float3 ltwPosition)
        {
            var distanceNearest = float.MaxValue;
            var indexChoose     = -1;

            for (var i = 0; i < characterSetTakeDamages.Length; i++)
            {
                var character = characterSetTakeDamages[i];
                var dis       = math.distance(ltwPosition, character.position);

                if (dis < distanceNearest)
                {
                    distanceNearest = dis;
                    indexChoose     = i;
                }
            }

            return indexChoose;
        }
    }

    #endregion

    private struct CharacterSetTakeDamage
    {
        public float3 position;
        public Entity entity;
    }

    private struct TakeDamageItem
    {
        public int   index;
        public float damage;
    }

    private struct AvoidData
    {
        public Entity         entity;
        public LocalTransform lt;
        public ZombieInfo     info;
    }
}