using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace _Game_.Scripts.Systems.Other.Obstacle
{
    [BurstCompile, UpdateInGroup(typeof(InitializationSystemGroup))]
    public partial struct TurretSystem : ISystem
    {
        private NativeArray<BufferObstacle> _bufferTurretObstacles;
        private NativeList<TargetAimInfo> _targetAimInfos;
        private NativeQueue<BufferBulletSpawner> _bulletSpawnQueue;
        private EntityQuery _enQueryBarrelInfo;
        private Entity _entityWeaponAuthoring;
        private EntityManager _entityManager;
        private bool _isInit;
        private ComponentTypeHandle<LocalTransform> _ltTypeHandle;
        private ComponentTypeHandle<BarrelInfo> _barrelInfoTypeHandle;
        private ComponentTypeHandle<BarrelRunTime> _barrelRunTimeTypeHandle;
        private ComponentTypeHandle<LocalToWorld> _ltwTypeHandle;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            RequireNecessaryComponents(ref state);
            Init(ref state);
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            CheckAndInitRunTime(ref state);
            CheckSetupBarrel(ref state);
            PutEventSpawnBullet(ref state);
            CheckAndHandleTimeLifeExpired(ref state);
        }

        [BurstCompile]
        private void CheckAndHandleTimeLifeExpired(ref SystemState state)
        {
            EntityCommandBuffer ecb = new EntityCommandBuffer(Allocator.Temp);
            float time = (float)SystemAPI.Time.ElapsedTime;
            var setActiveComponent = new SetActiveSP()
            {
                state = DisableID.Disable,
            };
            foreach (var (turret,entity) in SystemAPI.Query<RefRO<TurretInfo>>().WithEntityAccess().WithNone<Disabled, SetActiveSP>())
            {
                if (time - turret.ValueRO.startTime > turret.ValueRO.timeLife)
                {
                    ecb.AddComponent(entity,setActiveComponent);
                }
            }
            ecb.Playback(_entityManager);
            ecb.Dispose();
        }


        [BurstCompile]
        private void CheckSetupBarrel(ref SystemState state)
        {
            EntityCommandBuffer ecb = new EntityCommandBuffer(Allocator.Temp);
            foreach (var (barrelSetup, entity) in SystemAPI.Query<RefRO<BarrelCanSetup>>().WithEntityAccess()
                         .WithNone<Disabled>())
            {
                SetUpBarrel(ref ecb, entity, barrelSetup.ValueRO.id);
            }

            ecb.Playback(_entityManager);
            ecb.Dispose();
        }

        [BurstCompile]
        private BufferObstacle GetObstacle(int id)
        {
            BufferObstacle turret = default;

            foreach (var t in _bufferTurretObstacles)
            {
                if (t.id == id) return t;
            }

            return turret;
        }
        [BurstCompile]
        private void PutEventSpawnBullet(ref SystemState state)
        {
            _bulletSpawnQueue.Clear();
            UpdateTargetAimInfos(ref state);
            _ltTypeHandle.Update(ref state);
            _ltwTypeHandle.Update(ref state);
            _barrelInfoTypeHandle.Update(ref state);
            _barrelRunTimeTypeHandle.Update(ref state);
            
            var job = new BarreJOB()
            {
                ltComponentType = _ltTypeHandle,
                targetAimInfos = _targetAimInfos,
                barrelInfoComponentType = _barrelInfoTypeHandle,
                deltaTime = SystemAPI.Time.DeltaTime,
                barrelRunTimeComponentType = _barrelRunTimeTypeHandle,
                time = (float)SystemAPI.Time.ElapsedTime,
                ltwComponentTypeHandle = _ltwTypeHandle,
                bulletSpawnQueue = _bulletSpawnQueue.AsParallelWriter(),
            };
            state.Dependency = job.ScheduleParallel(_enQueryBarrelInfo, state.Dependency);
            state.Dependency.Complete();
            if (_bulletSpawnQueue.Count <= 0) return;
            var bufferSpawnBullet = state.EntityManager.GetBuffer<BufferBulletSpawner>(_entityWeaponAuthoring);
            while (_bulletSpawnQueue.TryDequeue(out var queue))
            {
                bufferSpawnBullet.Add(queue);
            }
        }

        [BurstCompile]
        private void UpdateTargetAimInfos(ref SystemState state)
        {
            _targetAimInfos.Clear();
            foreach (var (ltw,info) in SystemAPI.Query<RefRO<LocalToWorld>,RefRO<ZombieInfo>>()
                         .WithNone<Disabled, SetActiveSP,AddToBuffer>())
            {
                _targetAimInfos.Add(new TargetAimInfo()
                {
                    direct = info.ValueRO.currentDirect,
                    position = ltw.ValueRO.Position,
                    speed = info.ValueRO.speed,
                });
            }
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {
            if (_targetAimInfos.IsCreated)
                _targetAimInfos.Dispose();
            if (_bulletSpawnQueue.IsCreated)
                _bulletSpawnQueue.Dispose();
            if (_bufferTurretObstacles.IsCreated)
                _bufferTurretObstacles.Dispose();
        }

        [BurstCompile]
        private void RequireNecessaryComponents(ref SystemState state)
        {
            state.RequireForUpdate<TurretInfo>();
            state.RequireForUpdate<WeaponProperty>();
        }

        [BurstCompile]
        private void Init(ref SystemState state)
        {
            _targetAimInfos = new NativeList<TargetAimInfo>(Allocator.Persistent);
            _enQueryBarrelInfo = SystemAPI.QueryBuilder().WithAll<BarrelInfo, BarrelRunTime>()
                .WithNone<Disabled, SetActiveSP>().Build();
            _bulletSpawnQueue = new NativeQueue<BufferBulletSpawner>(Allocator.Persistent);
            _ltTypeHandle = state.GetComponentTypeHandle<LocalTransform>();
            _ltwTypeHandle = state.GetComponentTypeHandle<LocalToWorld>();
            _barrelInfoTypeHandle = state.GetComponentTypeHandle<BarrelInfo>();
            _barrelRunTimeTypeHandle = state.GetComponentTypeHandle<BarrelRunTime>();
        }
        
        [BurstCompile]
        private void CheckAndInitRunTime(ref SystemState state)
        {
            _entityManager = state.EntityManager;
            if (!_isInit)
            {
                _isInit = true;
                _entityWeaponAuthoring = SystemAPI.GetSingletonEntity<WeaponProperty>();
                _bufferTurretObstacles = SystemAPI.GetSingletonBuffer<BufferObstacle>()
                    .ToNativeArray(Allocator.Persistent);
            }
        }
        
        [BurstCompile]
        private BarrelInfo GetBarrelInfoFromBuffer(int id)
        {
            var obstacle = GetObstacle(id);
            var turret = obstacle.turretObstacle;
            var barrel = new BarrelInfo()
            {
                bulletPerShot = turret.bulletPerShot,
                cooldown = obstacle.cooldown,
                damage = obstacle.damage,
                distanceAim = turret.distanceAim,
                moveToWardMax = turret.moveToWardMax,
                moveToWardMin = turret.moveToWardMin,
                parallelOrbit = turret.parallelOrbit,
                pivotFireOffset = turret.pivotFireOffset,
                speed = obstacle.speed,
                spaceAnglePerBullet = turret.spaceAnglePerBullet,
                spacePerBullet = turret.spacePerBullet,
            };
            return barrel;
        }

        [BurstCompile]
        private void SetUpBarrel(ref EntityCommandBuffer ecb,Entity entity, int id)
        {
            ecb.AddComponent(entity, GetBarrelInfoFromBuffer(id));
            ecb.AddComponent<BarrelRunTime>(entity);
            ecb.RemoveComponent<BarrelCanSetup>(entity);
        }
        #region JOB

        [BurstCompile]
        partial struct BarreJOB : IJobChunk
        {
            public ComponentTypeHandle<LocalTransform> ltComponentType;
            public ComponentTypeHandle<BarrelRunTime> barrelRunTimeComponentType;
            public NativeQueue<BufferBulletSpawner>.ParallelWriter bulletSpawnQueue;
            [ReadOnly] public ComponentTypeHandle<LocalToWorld> ltwComponentTypeHandle;
            [ReadOnly] public NativeList<TargetAimInfo> targetAimInfos;
            [ReadOnly] public ComponentTypeHandle<BarrelInfo> barrelInfoComponentType;
            [ReadOnly] public float deltaTime;
            [ReadOnly] public float time;

            public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask,
                in v128 chunkEnabledMask)
            {
                var lts = chunk.GetNativeArray(ref ltComponentType);
                var barrelInfos = chunk.GetNativeArray(ref barrelInfoComponentType);
                var barrelRunTimes = chunk.GetNativeArray(ref barrelRunTimeComponentType);
                var ltws = chunk.GetNativeArray(ref ltwComponentTypeHandle);
                var lt_bullet = new LocalTransform()
                {
                    Scale = 1
                };
                for (int i = 0; i < chunk.Count; i++)
                {
                    var lt = lts[i];
                    var ltw = ltws[i];
                    var info = barrelInfos[i];
                    var moveToWard = info.moveToWardMax;
                    var direct = math.forward();
                    UpdateDataAim(ltw.Position, info, ref direct, ref moveToWard);
                    lt.Rotation = MathExt.MoveTowards(ltw.Rotation, quaternion.LookRotationSafe(direct, math.up()),
                        moveToWard * deltaTime);
                    lts[i] = lt;

                    var barrelRunTime = barrelRunTimes[i];
                    if ((time - barrelRunTime.value) > info.cooldown)
                    {
                        lt_bullet.Position = lt.TransformPoint(info.pivotFireOffset) + ltw.Position;
                        lt_bullet.Rotation = lt.Rotation;
                        bulletSpawnQueue.Enqueue(new BufferBulletSpawner()
                        {
                            bulletPerShot = info.bulletPerShot,
                            damage = info.damage,
                            lt = lt_bullet,
                            parallelOrbit = info.parallelOrbit,
                            speed = info.speed,
                            spaceAnglePerBullet = info.spaceAnglePerBullet,
                            spacePerBullet = info.spacePerBullet,
                            right = ltw.Right
                        });
                        barrelRunTime.value = time;
                        barrelRunTimes[i] = barrelRunTime;
                    }
                }
            }

            private void UpdateDataAim(float3 pos, BarrelInfo info, ref float3 direct,
                ref float moveToWard)
            {
                float distanceNearest = float.MaxValue;
                float3 positionNearest = float3.zero;
                bool check = false;
                float3 directTarget = float3.zero;
                float speed = 0f;
                
                foreach (var enemyPos in targetAimInfos)
                {
                    var distance = math.distance(pos, enemyPos.position);
                    if (distance < distanceNearest)
                    {
                        distanceNearest = distance;
                        positionNearest = enemyPos.position;
                        speed = enemyPos.speed;
                        directTarget = enemyPos.direct;
                        check = true;
                    }
                }

                if (check)
                {
                    
                    var mulValue = math.remap(1, info.distanceAim, 6, 2, distanceNearest);

                    mulValue = math.max(mulValue, 1);
                    
                    var posTarget = positionNearest + directTarget * speed * deltaTime * mulValue;
                    
                    direct = posTarget - pos;
                    moveToWard = math.remap(info.distanceAim,0,info.moveToWardMin,info.moveToWardMax,distanceNearest);
                }
                else
                {
                    moveToWard = info.moveToWardMax;
                    direct = math.forward();
                }
            }
        }


        struct TargetAimInfo
        {
            public float3 position;
            public float3 direct;
            public float speed;
        }

        #endregion
    }
}