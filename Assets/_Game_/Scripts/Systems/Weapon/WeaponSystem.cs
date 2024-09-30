
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

[BurstCompile,UpdateInGroup(typeof(InitializationSystemGroup))]
public partial struct WeaponSystem : ISystem
{
    private Entity _weaponEntityInstantiate;
    private EntityManager _entityManager;
    private Entity _entityWeaponAuthoring;
    private WeaponProperty _weaponProperties;
    private float _timeLatest;

    private bool _isInit;
    private bool _pullTrigger;
    private int _idCurrentWeapon;
    private float3 _offset;
    private float _cooldown;
    private int _bulletPerShot;
    private float _spacePerBullet;
    private float _damage;
    private float _speed;
    private float _spaceAnglePerBullet;
    private bool _parallelOrbit;

    private bool _isNewWeapon;
    private EntityQuery _enQueryWeapon;
    private NativeArray<BufferWeaponStore> _weaponStores;
    private NativeQueue<BufferBulletSpawner> _bulletSpawnQueue;
    private ComponentTypeHandle<LocalToWorld> _ltwTypeHandle;

    #region OnCreate

    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        RequireNecessaryComponents(ref state);
        Init(ref state);
    }
    [BurstCompile]
    private void Init(ref SystemState state)
    {
        _ltwTypeHandle = state.GetComponentTypeHandle<LocalToWorld>();
        _bulletSpawnQueue = new NativeQueue<BufferBulletSpawner>(Allocator.Persistent);
        _enQueryWeapon = SystemAPI.QueryBuilder().WithAll<WeaponInfo>().WithNone<Disabled, SetActiveSP>().Build();
        _idCurrentWeapon = -1;
    }
    [BurstCompile]
    private void RequireNecessaryComponents(ref SystemState state)
    {
        state.RequireForUpdate<PlayerInput>();
        state.RequireForUpdate<WeaponProperty>();
        state.RequireForUpdate<PlayerInfo>();
        state.RequireForUpdate<CharacterInfo>();
        state.RequireForUpdate<DataProperty>();
    }

    #endregion
    
    [BurstCompile]
    public void OnDestroy(ref SystemState state)
    {
        if (_bulletSpawnQueue.IsCreated)
            _bulletSpawnQueue.Dispose();
        if (_weaponStores.IsCreated)
            _weaponStores.Dispose();
    }


    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        if (!_isInit)
        {
            _isInit = true;
            InitUpdate(ref state);
            return;
        }
        if (!_weaponProperties.shootAuto)
        {
            _pullTrigger = SystemAPI.GetSingleton<PlayerInput>().pullTrigger;
        }
        Shot(ref state);
        UpdateDataWeapon(ref state);
        UpdateWeapon(ref state);
    }
    
    [BurstCompile]
    private void InitUpdate(ref SystemState state)
    {
        EntityCommandBuffer ecb = new EntityCommandBuffer(Allocator.TempJob);
        _entityWeaponAuthoring = SystemAPI.GetSingletonEntity<WeaponProperty>();
        _weaponProperties = SystemAPI.GetSingleton<WeaponProperty>();
        _pullTrigger = _weaponProperties.shootAuto;
        _entityManager = state.EntityManager;

        _weaponStores = SystemAPI.GetSingletonBuffer<BufferWeaponStore>().ToNativeArray(Allocator.Persistent);
        int getId = SystemAPI.GetSingleton<PlayerInfo>().idWeapon;
        if (_idCurrentWeapon != getId)
        {
            ChangeWeapon(getId,ref ecb);
        }
        ecb.Playback(_entityManager);
        ecb.Dispose();
    }
    [BurstCompile]
    private void UpdateDataWeapon(ref SystemState state)
    {
        EntityCommandBuffer ecb = new EntityCommandBuffer(Allocator.TempJob);
        foreach (var (collection,entity) in SystemAPI.Query<RefRO<ItemCollection>>().WithEntityAccess()
                     .WithNone<Disabled, SetActiveSP>())
        {
            switch (collection.ValueRO.type)
            {
                case ItemType.Weapon:
                    
                    if (_idCurrentWeapon != collection.ValueRO.id)
                    {
                        ChangeWeapon(collection.ValueRO.id,ref ecb);
                    }
                    ecb.DestroyEntity(entity);
                    break;
            }
        }
        ecb.Playback(_entityManager);
        ecb.Dispose();
    }
    [BurstCompile]
    private void ChangeWeapon(int id,ref EntityCommandBuffer ecb)
    {
        _idCurrentWeapon = id;
        if(id < 0) return;
        var weapon = GetWeapon(id);
        _offset = weapon.offset;
        _bulletPerShot = weapon.bulletPerShot;
        _spacePerBullet = weapon.spacePerBullet;
        _cooldown = weapon.cooldown;
        _timeLatest = -_cooldown;
        _weaponEntityInstantiate = weapon.entity;
        _spaceAnglePerBullet = weapon.spaceAnglePerBullet;
        _parallelOrbit = weapon.parallelOrbit;
        _damage = weapon.damage;
        _speed = weapon.speed;
        _isNewWeapon = true;
    }
    [BurstCompile]
    private BufferWeaponStore GetWeapon(int id)
    {
        BufferWeaponStore weaponStore = new BufferWeaponStore();

        foreach (var ws in _weaponStores)
        {
            if(ws.id != id) continue;
            weaponStore = ws;
            break;
        }
        
        return weaponStore;
    }

    [BurstCompile]
    private void UpdateWeapon(ref SystemState state)
    {
        if(_idCurrentWeapon < 0 )return;
        EntityCommandBuffer ecb = new EntityCommandBuffer(Allocator.TempJob);
        if (_isNewWeapon)
        {
            foreach (var (weaponInfo,parent, entity) in SystemAPI.Query<RefRO<WeaponInfo>,RefRO<Parent>>().WithEntityAccess()
                         .WithNone<Disabled,AddToBuffer,CanWeapon>())
            {
                ecb.RemoveComponent<Parent>(entity);
                ecb.AddComponent(entity,new SetActiveSP()
                {
                    state = DisableID.Disable,
                });

                InitCurrentWeapon(ref ecb, parent.ValueRO.Value);
            }
            _isNewWeapon = false;
        }
        
        foreach (var(info,entity) in SystemAPI.Query<RefRO<CharacterInfo>>().WithEntityAccess().WithAll<CanWeapon>().WithNone<Disabled,AddToBuffer>())
        {
            InitCurrentWeapon(ref ecb,entity);
            ecb.RemoveComponent<CanWeapon>(entity);
        }
        ecb.Playback(_entityManager);
        ecb.Dispose();
    }

    [BurstCompile]
    private void InitCurrentWeapon(ref EntityCommandBuffer ecb, Entity character)
    {
        Entity weaponEntity = _entityManager.Instantiate(_weaponEntityInstantiate);
        ecb.AddComponent(weaponEntity, new Parent() { Value = character });
        ecb.AddComponent(weaponEntity, new LocalTransform() { Position = _offset, Rotation = quaternion.identity, Scale = 1 });
        ecb.AddComponent(weaponEntity,new WeaponInfo()
        {
            id = _idCurrentWeapon,
        });
        var characterInfo = _entityManager.GetComponentData<CharacterInfo>(character);
        characterInfo.weaponEntity = weaponEntity;
        _entityManager.SetComponentData(character,characterInfo);
    }


    [BurstCompile]
    private void Shot(ref SystemState state)
    {
        if(!_pullTrigger) return;
        if ((SystemAPI.Time.ElapsedTime - _timeLatest) < _cooldown) return;
        _bulletSpawnQueue.Clear();
        _ltwTypeHandle.Update(ref state);
        
        var job = new PutEventSpawnBulletJOB()
        {
            bulletPerShot = _bulletPerShot,
            spacePerBullet = _spacePerBullet,
            damage = _damage,
            speed = _speed,
            spaceAnglePerBullet = _spaceAnglePerBullet,
            parallelOrbit = _parallelOrbit,
            bulletSpawnQueue = _bulletSpawnQueue.AsParallelWriter(),
            ltwComponentTypeHandle = _ltwTypeHandle
        };
        state.Dependency = job.ScheduleParallel(_enQueryWeapon, state.Dependency);
        state.Dependency.Complete();
        _timeLatest = (float)SystemAPI.Time.ElapsedTime;
        if (_bulletSpawnQueue.Count > 0)
        {
            var bufferSpawnBullet = state.EntityManager.AddBuffer<BufferBulletSpawner>(_entityWeaponAuthoring);
            while(_bulletSpawnQueue.TryDequeue(out var queue))
            {
                bufferSpawnBullet.Add(queue);
            }
        }
    }
    
    [BurstCompile]
    partial struct PutEventSpawnBulletJOB : IJobChunk
    {
        public int bulletPerShot;
        public float damage;
        public float speed;
        public float spaceAnglePerBullet;
        public float spacePerBullet;
        public bool parallelOrbit;
        public NativeQueue<BufferBulletSpawner>.ParallelWriter bulletSpawnQueue;
        [ReadOnly] public ComponentTypeHandle<LocalToWorld> ltwComponentTypeHandle;
        
        public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
        {
            var ltws = chunk.GetNativeArray(ref ltwComponentTypeHandle);

            for (var i = 0; i < chunk.Count; i++)
            {
                var ltw = ltws[i];
                bulletSpawnQueue.Enqueue(new BufferBulletSpawner()
                {
                    bulletPerShot = bulletPerShot,
                    damage = damage,
                    spacePerBullet = spacePerBullet,
                    parallelOrbit = parallelOrbit,
                    speed = speed,
                    spaceAnglePerBullet = spaceAnglePerBullet,
                    lt = new LocalTransform()
                    {
                        Position = ltw.Position,
                        Rotation = ltw.Rotation,
                        Scale = 1,
                    },
                    right = ltw.Right
                });
            }
            
        }
    }

}
