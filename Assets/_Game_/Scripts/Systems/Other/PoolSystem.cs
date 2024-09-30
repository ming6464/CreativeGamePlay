using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;

//
[BurstCompile, UpdateInGroup(typeof(PresentationSystemGroup)), UpdateAfter(typeof(HandleSetActiveSystem))]
public partial struct HandlePoolZombie : ISystem
{
    private NativeList<BufferZombieDie> _zombieDieToPoolList;
    private EntityQuery _entityQuery;
    private EntityTypeHandle _entityTypeHandle;
    private EntityManager _entityManager;
    private ZombieProperty _zombieProperty;
    private ComponentTypeHandle<ZombieInfo> _zombieInfoTypeHandle;
    private bool _isInit;

    private int _currentCountZombieDie;
    private int _passCountZombieDie;
    private int _countCheck;

    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        RequireNecessaryComponents(ref state);
        Init(ref state);
    }

    [BurstCompile]
    private void Init(ref SystemState state)
    {
        _countCheck = 300;
        _entityQuery = SystemAPI.QueryBuilder().WithAll<ZombieInfo, AddToBuffer, Disabled>().Build();
        _entityTypeHandle = state.GetEntityTypeHandle();
        _zombieInfoTypeHandle = state.GetComponentTypeHandle<ZombieInfo>();
    }

    [BurstCompile]
    private void RequireNecessaryComponents(ref SystemState state)
    {
        state.RequireForUpdate<ZombieProperty>();
        state.RequireForUpdate<AddToBuffer>();
    }

    [BurstCompile]
    public void OnDestroy(ref SystemState state)
    {
        if (_zombieDieToPoolList.IsCreated)
            _zombieDieToPoolList.Dispose();
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        _entityManager = state.EntityManager;
        CheckAndInit();
        LoadZombieToPool(ref state);
    }

    [BurstCompile]
    private void LoadZombieToPool(ref SystemState state)
    {
        EntityCommandBuffer ecb = new EntityCommandBuffer(Allocator.TempJob);
        _entityTypeHandle.Update(ref state);
        _zombieInfoTypeHandle.Update(ref state);
        var job = new GetListZombieDataToPool()
        {
            zombieDieToPoolList = _zombieDieToPoolList.AsParallelWriter(),
            entityTypeHandle = _entityTypeHandle,
            zombieInfoTypeHandle = _zombieInfoTypeHandle,
            ecb = ecb.AsParallelWriter(),
        };
        state.Dependency = job.ScheduleParallel(_entityQuery, state.Dependency);
        state.Dependency.Complete();
        ecb.Playback(_entityManager);
        ecb.Dispose();
        if (_countCheck < (_zombieDieToPoolList.Length - _passCountZombieDie))
        {
            _countCheck = _zombieDieToPoolList.Length - _passCountZombieDie + 300;
        }

        _passCountZombieDie = _zombieDieToPoolList.Length;
        var arrCopy = _zombieDieToPoolList.ToArray(Allocator.Temp);
        _entityManager.GetBuffer<BufferZombieDie>(_zombieProperty.entity).AddRange(arrCopy);
        arrCopy.Dispose();
        if (_passCountZombieDie > 0)
        {
            var runtime = _entityManager.GetComponentData<ZombieSpawnRuntime>(_zombieProperty.entity);
            runtime.zombieAlive -= _passCountZombieDie;
            _entityManager.SetComponentData(_zombieProperty.entity, runtime);
        }
    }

    [BurstCompile]
    private void CheckAndInit()
    {
        if (!_isInit)
        {
            _isInit = true;
            _zombieProperty = SystemAPI.GetSingleton<ZombieProperty>();
            _currentCountZombieDie = 500;
            _zombieDieToPoolList = new NativeList<BufferZombieDie>(_currentCountZombieDie, Allocator.Persistent);
        }

        if (_currentCountZombieDie - _passCountZombieDie < _countCheck)
        {
            _zombieDieToPoolList.Dispose();
            _currentCountZombieDie = _passCountZombieDie + _countCheck;
            _zombieDieToPoolList = new NativeList<BufferZombieDie>(_currentCountZombieDie, Allocator.Persistent);
        }
        else
        {
            _zombieDieToPoolList.Clear();
        }
    }

    [BurstCompile]
    partial struct GetListZombieDataToPool : IJobChunk
    {
        public EntityCommandBuffer.ParallelWriter ecb;
        [WriteOnly] public NativeList<BufferZombieDie>.ParallelWriter zombieDieToPoolList;
        [ReadOnly] public EntityTypeHandle entityTypeHandle;
        [ReadOnly] public ComponentTypeHandle<ZombieInfo> zombieInfoTypeHandle;

        public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask,
            in v128 chunkEnabledMask)
        {
            var entities = chunk.GetNativeArray(entityTypeHandle);
            var zombieInfos = chunk.GetNativeArray(ref zombieInfoTypeHandle);

            for (int i = 0; i < chunk.Count; i++)
            {
                var entity = entities[i];
                var zombieInfo = zombieInfos[i];
                zombieDieToPoolList.AddNoResize(new BufferZombieDie()
                {
                    id = zombieInfo.id,
                    entity = entity,
                });
            }

            ecb.RemoveComponent<AddToBuffer>(unfilteredChunkIndex, entities);
        }
    }
}

[BurstCompile, UpdateInGroup(typeof(PresentationSystemGroup))]
public partial struct HandlePoolBullet : ISystem
{
    private NativeList<BufferBulletDisable> _bufferBulletDisables;
    private Entity _entityWeaponProperty;
    private EntityQuery _entityQuery;
    private EntityTypeHandle _entityTypeHandle;
    private EntityManager _entityManager;
    private bool _isInit;

    private int _currentCountWeaponDisable;
    private int _passCountWeaponDisable;
    private int _countCheck;

    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        RequiredNecessary(ref state);
        Init(ref state);
    }

    [BurstCompile]
    private void Init(ref SystemState state)
    {
        _countCheck = 300;
        _entityQuery = SystemAPI.QueryBuilder().WithAll<BulletInfo, AddToBuffer, Disabled>().Build();
        _bufferBulletDisables = new NativeList<BufferBulletDisable>(_countCheck, Allocator.Persistent);
        _currentCountWeaponDisable = _countCheck;
        _entityTypeHandle = state.GetEntityTypeHandle();
    }

    [BurstCompile]
    private void RequiredNecessary(ref SystemState state)
    {
        state.RequireForUpdate<WeaponProperty>();
        state.RequireForUpdate<AddToBuffer>();
    }

    [BurstCompile]
    public void OnDestroy(ref SystemState state)
    {
        if (_bufferBulletDisables.IsCreated)
            _bufferBulletDisables.Dispose();
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        if (!CheckAndInit(ref state)) return;
        if (_currentCountWeaponDisable - _passCountWeaponDisable < _countCheck)
        {
            _bufferBulletDisables.Dispose();
            _currentCountWeaponDisable = _passCountWeaponDisable + _countCheck;
            _bufferBulletDisables =
                new NativeList<BufferBulletDisable>(_currentCountWeaponDisable, Allocator.Persistent);
        }
        else
        {
            _bufferBulletDisables.Clear();
        }

        _entityTypeHandle.Update(ref state);
        EntityCommandBuffer ecb = new EntityCommandBuffer(Allocator.TempJob);
        var job = new GetListDataToPool()
        {
            bulletToPoolList = _bufferBulletDisables.AsParallelWriter(),
            entityTypeHandle = _entityTypeHandle,
            ecb = ecb.AsParallelWriter(),
        };
        state.Dependency = job.ScheduleParallel(_entityQuery, state.Dependency);
        state.Dependency.Complete();
        ecb.Playback(_entityManager);
        ecb.Dispose();

        if (_countCheck < (_bufferBulletDisables.Length - _passCountWeaponDisable))
        {
            _countCheck += _bufferBulletDisables.Length - _passCountWeaponDisable;
        }

        _passCountWeaponDisable = _bufferBulletDisables.Length;
        var arrayCopy = _bufferBulletDisables.ToArray(Allocator.Temp);
        _entityManager.GetBuffer<BufferBulletDisable>(_entityWeaponProperty).AddRange(arrayCopy);
        arrayCopy.Dispose();
    }

    [BurstCompile]
    private bool CheckAndInit(ref SystemState state)
    {
        if (_isInit) return true;
        _isInit = true;
        _entityWeaponProperty = SystemAPI.GetSingletonEntity<WeaponProperty>();
        _entityManager = state.EntityManager;
        return false;
    }


    [BurstCompile]
    partial struct GetListDataToPool : IJobChunk
    {
        public EntityCommandBuffer.ParallelWriter ecb;
        [WriteOnly] public NativeList<BufferBulletDisable>.ParallelWriter bulletToPoolList;
        [ReadOnly] public EntityTypeHandle entityTypeHandle;

        public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask,
            in v128 chunkEnabledMask)
        {
            var entities = chunk.GetNativeArray(entityTypeHandle);

            for (int i = 0; i < chunk.Count; i++)
            {
                var entity = entities[i];
                bulletToPoolList.AddNoResize(new BufferBulletDisable()
                {
                    entity = entity,
                });
            }

            ecb.RemoveComponent<AddToBuffer>(unfilteredChunkIndex, entities);
        }
    }
}



#region Command

//
//

// [BurstCompile, UpdateInGroup(typeof(PresentationSystemGroup)), UpdateAfter(typeof(HandleSetActiveSystem))]
// public partial struct HandlePoolCharacter : ISystem
// {
//     private NativeList<BufferCharacterDie> _characterDieToPoolList;
//     private Entity _entityPlayerInfo;
//     private EntityQuery _entityQuery;
//     private EntityTypeHandle _entityTypeHandle;
//     private EntityManager _entityManager;
//     private bool _isInit;
//
//     private int _currentCountCharacterDie;
//     private int _passCountCharacterDie;
//     private int _countCheck;
//
//     [BurstCompile]
//     public void OnCreate(ref SystemState state)
//     {
//         state.RequireForUpdate<CharacterInfo>();
//         state.RequireForUpdate<AddToBuffer>();
//         state.RequireForUpdate<Disabled>();
//         _countCheck = 20;
//         _entityQuery = SystemAPI.QueryBuilder().WithAll<CharacterInfo, AddToBuffer, Disabled>().Build();
//         _entityTypeHandle = state.GetEntityTypeHandle();
//     }
//
//     [BurstCompile]
//     public void OnDestroy(ref SystemState state)
//     {
//         if (_characterDieToPoolList.IsCreated)
//             _characterDieToPoolList.Dispose();
//     }
//
//     [BurstCompile]
//     public void OnUpdate(ref SystemState state)
//     {
//         return;
//         _entityManager = state.EntityManager;
//         if (!_isInit)
//         {
//             _isInit = true;
//             _entityPlayerInfo = SystemAPI.GetSingletonEntity<PlayerInfo>();
//             _currentCountCharacterDie = 0;
//             _characterDieToPoolList =
//                 new NativeList<BufferCharacterDie>(_currentCountCharacterDie, Allocator.Persistent);
//         }
//
//         if (_currentCountCharacterDie - _passCountCharacterDie < _countCheck)
//         {
//             _characterDieToPoolList.Dispose();
//             _currentCountCharacterDie = _passCountCharacterDie + _countCheck;
//             _characterDieToPoolList =
//                 new NativeList<BufferCharacterDie>(_currentCountCharacterDie, Allocator.Persistent);
//             return;
//         }
//         else
//         {
//             _characterDieToPoolList.Clear();
//         }
//
//         _entityTypeHandle.Update(ref state);
//         EntityCommandBuffer ecb = new EntityCommandBuffer(Allocator.TempJob);
//         var job = new GetListCharacterDataToPool()
//         {
//             characterDieToPoolList = _characterDieToPoolList.AsParallelWriter(),
//             entityTypeHandle = _entityTypeHandle,
//             ecb = ecb.AsParallelWriter(),
//         };
//         state.Dependency = job.ScheduleParallel(_entityQuery, state.Dependency);
//         state.Dependency.Complete();
//         ecb.Playback(_entityManager);
//         ecb.Dispose();
//
//         if (_countCheck < (_characterDieToPoolList.Length - _passCountCharacterDie))
//         {
//             _countCheck = _characterDieToPoolList.Length - _passCountCharacterDie + 10;
//         }
//
//         _passCountCharacterDie = _characterDieToPoolList.Length;
//         _entityManager.GetBuffer<BufferCharacterDie>(_entityPlayerInfo).AddRange(_characterDieToPoolList);
//     }
//
//     [BurstCompile]
//     partial struct GetListCharacterDataToPool : IJobChunk
//     {
//         public EntityCommandBuffer.ParallelWriter ecb;
//         [WriteOnly] public NativeList<BufferCharacterDie>.ParallelWriter characterDieToPoolList;
//         [ReadOnly] public EntityTypeHandle entityTypeHandle;
//
//         public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask,
//             in v128 chunkEnabledMask)
//         {
//             var entities = chunk.GetNativeArray(entityTypeHandle);
//
//             for (int i = 0; i < chunk.Count; i++)
//             {
//                 var entity = entities[i];
//                 characterDieToPoolList.AddNoResize(new BufferCharacterDie()
//                 {
//                     entity = entity,
//                 });
//             }
//
//             ecb.RemoveComponent<AddToBuffer>(unfilteredChunkIndex, entities);
//         }
//     }
// }

//

#endregion