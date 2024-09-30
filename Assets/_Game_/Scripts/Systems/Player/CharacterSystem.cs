using System.Collections.Generic;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;
using UnityEngine;

namespace _Game_.Scripts.Systems.Player
{
    [BurstCompile, UpdateInGroup(typeof(InitializationSystemGroup))]
    public partial struct CharacterSystem : ISystem
    {
        private EntityQuery                         _enQueryCharacterTakeDamage;
        private EntityTypeHandle                    _entityTypeHandle;
        private EntityManager                       _entityManager;
        private NativeQueue<Entity>                 _characterDieQueue;
        private EntityQuery                         _enQueryMove;
        private EntityQuery                         _enQueryCharacterMove;
        private bool                                _isInit;
        private float                               _characterMoveToWardChangePos;
        private PlayerProperty                      _playerProperty;
        private NativeList<TargetInfo>              _targetNears;
        private PlayerInput                         _playerMoveInput;
        private float2                              _currentDirectMove;
        private ComponentTypeHandle<LocalToWorld>   _ltwTypeHandle;
        private ComponentTypeHandle<LocalTransform> _ltTypeHandle;
        private ComponentTypeHandle<TakeDamage>     _takeDamageTypeHandle;
        private ComponentTypeHandle<CharacterInfo>  _characterInfoTypeHandle;
        private ComponentTypeHandle<NextPoint>      _nextPointTypeHandle;
        private quaternion                          _currentQuaternion;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            RequireNecessaryComponents(ref state);
            Init(ref state);
        }
        [BurstCompile]
        private void RequireNecessaryComponents(ref SystemState state)
        {
            state.RequireForUpdate<PlayerProperty>();
            state.RequireForUpdate<PlayerInput>();
            state.RequireForUpdate<PlayerInfo>();
            state.RequireForUpdate<CharacterInfo>();
            state.RequireForUpdate<PhysicsWorldSingleton>();
            state.RequireForUpdate<LayerStoreComponent>();
        }

        [BurstCompile]
        private void Init(ref SystemState state)
        {
            _ltwTypeHandle = state.GetComponentTypeHandle<LocalToWorld>();
            _ltTypeHandle = state.GetComponentTypeHandle<LocalTransform>();
            _takeDamageTypeHandle = state.GetComponentTypeHandle<TakeDamage>();
            _characterInfoTypeHandle = state.GetComponentTypeHandle<CharacterInfo>();
            _nextPointTypeHandle = state.GetComponentTypeHandle<NextPoint>();
            _entityTypeHandle = state.GetEntityTypeHandle();
            _enQueryCharacterTakeDamage = SystemAPI.QueryBuilder().WithAll<CharacterInfo, TakeDamage>()
                .WithNone<Disabled, SetActiveSP, AddToBuffer>().Build();
            _characterDieQueue = new(Allocator.Persistent);
            _enQueryMove = SystemAPI.QueryBuilder().WithAll<CharacterInfo, NextPoint>()
                .WithNone<Disabled, SetActiveSP,AddToBuffer>()
                .Build();
            _enQueryCharacterMove = SystemAPI.QueryBuilder().WithAll<CharacterInfo>()
                .WithNone<Disabled, SetActiveSP, AddToBuffer>()
                .Build();
            _targetNears = new(Allocator.Persistent);
        }

        

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {
            if (_targetNears.IsCreated) _targetNears.Dispose();
            if (_characterDieQueue.IsCreated) _characterDieQueue.Dispose();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            if (!CheckAndInit(ref state)) return;
            HandleAnimation(ref state);
            CheckTakeDamage(ref state);
            Rota(ref state);
            Move(ref state);
        }

        [BurstCompile]
        private bool CheckAndInit(ref SystemState state)
        {
            if (!_isInit)
            {
                _isInit                       = true;
                _entityManager                = state.EntityManager;
                _playerProperty               = SystemAPI.GetSingleton<PlayerProperty>();
                _characterMoveToWardChangePos = _playerProperty.speedMoveToNextPoint;
            }

            _playerMoveInput = SystemAPI.GetSingleton<PlayerInput>();
            return true;
        }


        [BurstCompile]
        private void HandleAnimation(ref SystemState state)
        {
            EntityCommandBuffer ecb = new EntityCommandBuffer(Allocator.Temp);
            if (!_playerMoveInput.directMove.ComparisionEqual(_currentDirectMove))
            {
                _currentDirectMove = _playerMoveInput.directMove;
                StateID stateID = _currentDirectMove.ComparisionEqual(float2.zero) ? StateID.None : StateID.Run;
                foreach (var (characterInfo, entity) in SystemAPI.Query<RefRO<CharacterInfo>>().WithEntityAccess()
                             .WithNone<Disabled, SetAnimationSP>())
                {
                    ecb.AddComponent(entity, new SetAnimationSP()
                    {
                        state = stateID,
                    });
                }
            }
            else
            {
                StateID stateID = _currentDirectMove.ComparisionEqual(float2.zero) ? StateID.None : StateID.Run;

                foreach (var (info, entity) in SystemAPI.Query<RefRO<CharacterInfo>>().WithEntityAccess().WithAll<New>()
                             .WithNone<Disabled, AddToBuffer>())
                {
                    ecb.AddComponent(entity, new SetAnimationSP()
                    {
                        state = stateID,
                    });
                    ecb.RemoveComponent<New>(entity);
                }
            }

            ecb.Playback(_entityManager);
            ecb.Dispose();
        }

        [BurstCompile]
        private void Rota(ref SystemState state)
        {
            var playerLtw = SystemAPI.GetComponentRO<LocalToWorld>(SystemAPI.GetSingletonEntity<PlayerInfo>()).ValueRO;
            var directRota = math.forward();
            var distanceNearest = float.MaxValue;
            var positionAim = float3.zero;
            var direct = float3.zero;
            var speed = 0.0f;
            var moveToWard = _playerProperty.moveToWardMax;
            var deltaTime = SystemAPI.Time.DeltaTime;
            
            if (_playerProperty.aimNearestEnemy)
            {
                var check = UpdateTargetNears(ref state, playerLtw, ref distanceNearest, ref positionAim,
                        ref direct, ref speed);
                
                if (check)
                {
                    var mulValue = math.remap(0, _playerProperty.distanceAim, 7, 2, distanceNearest);
                    mulValue = math.max(mulValue, 1);
                    var positionNearestNextFrame = positionAim + direct * speed * deltaTime * mulValue;
                    directRota = positionNearestNextFrame - playerLtw.Position;
                    moveToWard = math.remap(_playerProperty.distanceAim, 0, _playerProperty.moveToWardMin,
                            _playerProperty.moveToWardMax, distanceNearest);
                    moveToWard = math.max(moveToWard, 0);
                    
                    // var playerInfo = SystemAPI.GetComponent<PlayerInfo>(_playerEntity);
                    // var nextQuaternion = quaternion.LookRotationSafe(directRota,math.up());
                    // var t              = moveToWard * SystemAPI.Time.DeltaTime;
                    // nextQuaternion = MathExt.MoveTowards(_currentQuaternion, nextQuaternion, t);
                    // playerInfo.rotateCharacter = nextQuaternion;
                    // _currentQuaternion         = nextQuaternion;
                    // SystemAPI.SetComponent(_playerEntity, playerInfo);
                }
            }
            
            
            _ltTypeHandle.Update(ref state);
            _ltwTypeHandle.Update(ref state);

            // Truyền NativeArray vào job
            var job = new CharacterRotaJOB()
            {
                    aimNearestEnemy  = _playerProperty.aimNearestEnemy,
                    ltComponentType  = _ltTypeHandle,
                    ltwComponentType = _ltwTypeHandle,
                    targetNears      = _targetNears,
                    playerProperty   = _playerProperty,
                    deltaTime        = SystemAPI.Time.DeltaTime,
                    directRota       = directRota,
                    moveToWard       = moveToWard,
                    rotaHelicopter   = _playerMoveInput.rotationOnHelicopterMode,
                    rota3D = _playerMoveInput.rota3D,
            };

            state.Dependency = job.ScheduleParallel(_enQueryCharacterMove, state.Dependency);
            state.Dependency.Complete();
        }

        [BurstCompile]
        private bool UpdateTargetNears(ref SystemState state, LocalToWorld playerLtw, ref float distanceNearest,
            ref float3 positionNearest, ref float3 direct, ref float speed)
        {
            _targetNears.Clear();
            bool check = false;
            foreach (var (ltw, zombieInfo) in SystemAPI.Query<RefRO<LocalToWorld>, RefRO<ZombieInfo>>()
                         .WithNone<Disabled, SetActiveSP, AddToBuffer>())
            {
                var posTarget = ltw.ValueRO.Position;
                if (MathExt.CalculateAngle(posTarget - playerLtw.Position, playerLtw.Forward) > 120) continue;
                float distance = math.distance(playerLtw.Position, posTarget);

                if (distance <= distanceNearest)
                {
                    check           = true;
                    distanceNearest = distance;
                    if (_playerProperty.aimType == AimType.TeamAim)
                    {
                        distanceNearest = distance;
                        direct = zombieInfo.ValueRO.currentDirect;
                        speed = zombieInfo.ValueRO.speed;
                        continue;
                    }
                    _targetNears.Add(new()
                    {
                        position = posTarget,
                        distance = distance,
                        direct = zombieInfo.ValueRO.currentDirect,
                        speed = zombieInfo.ValueRO.speed,
                    });
                    positionNearest = posTarget;
                }
            }
            foreach (var ltw in SystemAPI.Query<RefRO<LocalToWorld>>().WithAll<ItemCanShoot>()
                         .WithNone<Disabled, SetActiveSP>())
            {
                var posTarget = ltw.ValueRO.Position;
                if (MathExt.CalculateAngle(posTarget - playerLtw.Position, playerLtw.Forward) > 120) continue;
                float distance = math.distance(playerLtw.Position, posTarget);
                if (distance <= distanceNearest)
                {
                    check           = true;
                    distanceNearest = distance;
                    if (_playerProperty.aimType == AimType.TeamAim)
                    {
                        speed = 0;
                        continue;
                    }

                    _targetNears.Add(new()
                    {
                        position = ltw.ValueRO.Position,
                        distance = distance
                    });
                    positionNearest = posTarget;
                }
            }

            if (_targetNears.Length > 20)
            {
                _targetNears.Sort(new TargetInfoComparer());
                _targetNears.Resize(20, NativeArrayOptions.ClearMemory);
            }

            return check;
        }

        [BurstCompile]
        private void Move(ref SystemState state)
        {
            EntityCommandBuffer ecb = new EntityCommandBuffer(Allocator.TempJob);
            _entityTypeHandle.Update(ref state);
            var listNextPointEntity = _enQueryMove.ToEntityArray(Allocator.Temp);
            listNextPointEntity.Dispose();
            _ltTypeHandle.Update(ref state);
            _ltwTypeHandle.Update(ref state);
            _nextPointTypeHandle.Update(ref state);
            var job = new CharacterMoveNextPointJOB()
            {
                deltaTime = SystemAPI.Time.DeltaTime,
                ecb = ecb.AsParallelWriter(),
                entityTypeHandle = _entityTypeHandle,
                ltComponentType = _ltTypeHandle,
                moveToWardValue = _characterMoveToWardChangePos,
                nextPointComponentType = _nextPointTypeHandle
            };

            state.Dependency = job.ScheduleParallel(_enQueryMove, state.Dependency);
            state.Dependency.Complete();
            ecb.Playback(_entityManager);
            ecb.Dispose();
        }

        [BurstCompile]
        private void CheckTakeDamage(ref SystemState state)
        {
            EntityCommandBuffer ecb = new EntityCommandBuffer(Allocator.TempJob);
            _characterDieQueue.Clear();
            _entityTypeHandle.Update(ref state);
            _takeDamageTypeHandle.Update(ref state);
            _characterInfoTypeHandle.Update(ref state);
            var job = new CharacterHandleTakeDamageJOB()
            {
                ecb = ecb.AsParallelWriter(),
                characterInfoTypeHandle = _characterInfoTypeHandle,
                entityTypeHandle = _entityTypeHandle,
                takeDamageTypeHandle = _takeDamageTypeHandle,
                characterDieQueue = _characterDieQueue.AsParallelWriter(),
            };
            state.Dependency = job.ScheduleParallel(_enQueryCharacterTakeDamage, state.Dependency);
            state.Dependency.Complete();
            while (_characterDieQueue.TryDequeue(out var queue))
            {
                ecb.AddComponent(queue, new SetAnimationSP()
                {
                    state = StateID.Die,
                    timeDelay = 4,
                });
                ecb.AddComponent<AddToBuffer>(queue);
            }

            ecb.Playback(_entityManager);
            ecb.Dispose();
        }

        private struct TargetInfoComparer : IComparer<TargetInfo>
        {
            public int Compare(TargetInfo x, TargetInfo y)
            {
                return x.distance.CompareTo(y.distance);
            }
        }

        [BurstCompile]
        partial struct CharacterRotaJOB : IJobChunk
        {
            public ComponentTypeHandle<LocalTransform> ltComponentType;
            [ReadOnly]  public bool                                aimNearestEnemy;
            [ReadOnly]  public ComponentTypeHandle<LocalToWorld>   ltwComponentType;
            [ReadOnly]  public NativeList<TargetInfo>              targetNears;
            [ReadOnly]  public PlayerProperty                      playerProperty;
            [ReadOnly]  public float                               deltaTime;
            [ReadOnly]  public float3                              directRota;
            [ReadOnly]  public float                               moveToWard;
            [ReadOnly]
            public quaternion rotaHelicopter;

            [ReadOnly]
            public bool rota3D;

            public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask,
                in v128 chunkEnabledMask)
            {
                var lts = chunk.GetNativeArray(ref ltComponentType);
                var ltws = chunk.GetNativeArray(ref ltwComponentType);
                for (var i = 0; i < chunk.Count; i++)
                {
                    var lt = lts[i];

                    if (!aimNearestEnemy)
                    {
                        lt.Rotation = rotaHelicopter;
                    }
                    else
                    {
                        LoadDirectRota(out float3 directRota, out float moveToWard, ltws[i].Position);
                        if (!directRota.Equals(float3.zero))
                        {
                            var rotation = quaternion.LookRotationSafe(directRota, math.up());
                            lt.Rotation = MathExt.MoveTowards(lt.Rotation,rotation,
                                    deltaTime * moveToWard);
                        }
                    }
                    lts[i] = lt;
                }
            }

            private void LoadDirectRota(out float3 directRef, out float moveToWardRef, float3 characterPos)
            {
                directRef = directRota;
                moveToWardRef = moveToWard;
                
                var disNearest  = float.MaxValue;
                int indexChoose = -1;
                for (int i = 0; i < targetNears.Length; i++)
                {
                    var enemyPos = targetNears[i];
                    var distance = math.distance(enemyPos.position, characterPos);
                    if (distance < disNearest)
                    {
                        indexChoose = i;
                        disNearest  = distance;
                    }
                }

                if (indexChoose >= 0)
                {
                    var enemyTarget = targetNears[indexChoose];
                    var mulValue    = math.remap(0, playerProperty.distanceAim, 6, 2, disNearest);

                    mulValue = math.max(mulValue, 1);

                    var nearestEnemyPositionNextTime = enemyTarget.position +
                                                       enemyTarget.direct * deltaTime * enemyTarget.speed * mulValue;
                    directRef = nearestEnemyPositionNextTime - characterPos;
                    moveToWardRef = math.remap(playerProperty.distanceAim, 0, playerProperty.moveToWardMin,
                            playerProperty.moveToWardMax, disNearest);
                }

                if (!rota3D)
                {
                    directRef.y = 0;
                }
                
            }
        }

        [BurstCompile]
        partial struct CharacterHandleTakeDamageJOB : IJobChunk
        {
            public NativeQueue<Entity>.ParallelWriter characterDieQueue;
            public EntityCommandBuffer.ParallelWriter ecb;
            public ComponentTypeHandle<CharacterInfo> characterInfoTypeHandle;
            public EntityTypeHandle entityTypeHandle;
            [ReadOnly] public ComponentTypeHandle<TakeDamage> takeDamageTypeHandle;
            [ReadOnly] public float characterRadius;

            public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask,
                in v128 chunkEnabledMask)
            {
                var characterInfos = chunk.GetNativeArray(ref characterInfoTypeHandle);
                var takeDamages = chunk.GetNativeArray(ref takeDamageTypeHandle);
                var entities = chunk.GetNativeArray(entityTypeHandle);

                for (int i = 0; i < chunk.Count; i++)
                {
                    var info = characterInfos[i];
                    var takeDamage = takeDamages[i];
                    var entity = entities[i];
                    info.hp -= takeDamage.value;
                    characterInfos[i] = info;
                    ecb.RemoveComponent<TakeDamage>(unfilteredChunkIndex, entity);
                    if (info.hp <= 0)
                    {
                        ecb.RemoveComponent<LocalTransform>(unfilteredChunkIndex, entity);
                        ecb.RemoveComponent<Parent>(unfilteredChunkIndex, entity);
                        characterDieQueue.Enqueue(entity);
                        if (!info.weaponEntity.Equals(default))
                        {
                            ecb.RemoveComponent<Parent>(unfilteredChunkIndex, info.weaponEntity);
                            ecb.AddComponent(unfilteredChunkIndex, info.weaponEntity, new SetActiveSP()
                            {
                                state = DisableID.Disable,
                            });
                        }
                    }
                }
            }
        }

        [BurstCompile]
        partial struct CharacterMoveNextPointJOB : IJobChunk
        {
            public EntityCommandBuffer.ParallelWriter ecb;
            public EntityTypeHandle entityTypeHandle;
            public ComponentTypeHandle<LocalTransform> ltComponentType;
            [ReadOnly] public float deltaTime;
            [ReadOnly] public float moveToWardValue;
            [ReadOnly] public ComponentTypeHandle<NextPoint> nextPointComponentType;

            public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask,
                in v128 chunkEnabledMask)
            {
                var lts = chunk.GetNativeArray(ref ltComponentType);
                var nextPoints = chunk.GetNativeArray(ref nextPointComponentType);
                var entities = chunk.GetNativeArray(entityTypeHandle);
                
                if(lts.Length < chunk.Count) return;
                
                for (int i = 0; i < chunk.Count; i++)
                {
                    var lt = lts[i];
                    var nextPoint = nextPoints[i].value;
                    if (lt.Position.ComparisionEqual(nextPoint))
                    {
                        ecb.RemoveComponent<NextPoint>(unfilteredChunkIndex, entities[i]);
                        continue;
                    }

                    lt.Position = MathExt.MoveTowards(lt.Position, nextPoint, deltaTime * moveToWardValue);
                    lts[i] = lt;
                }
            }
        }

        private struct TargetInfo
        {
            public float3 position;
            public float distance;
            public float3 direct;
            public float speed;
        }
    }
}