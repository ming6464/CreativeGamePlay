using System;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;
using UnityEngine;

//
[BurstCompile, UpdateInGroup(typeof(PresentationSystemGroup))]
public partial struct HandleSetActiveSystem : ISystem
{
    private EntityTypeHandle _entityTypeHandle;
    private EntityQuery _enQuerySetActive;
    private ComponentTypeHandle<SetActiveSP> _setActiveSPTypeHandle;
    private BufferLookup<LinkedEntityGroup> _linkedBufferLookup;
    private BufferLookup<Child> _childBufferLookup;

    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        RequiredNecessary(ref state);
        Init(ref state);
    }

    [BurstCompile]
    private void Init(ref SystemState state)
    {
        _setActiveSPTypeHandle = state.GetComponentTypeHandle<SetActiveSP>();
        _linkedBufferLookup = state.GetBufferLookup<LinkedEntityGroup>();
        _childBufferLookup = state.GetBufferLookup<Child>();
        _entityTypeHandle = state.GetEntityTypeHandle();
        _enQuerySetActive = SystemAPI.QueryBuilder().WithAll<SetActiveSP>().Build();
    }

    [BurstCompile]
    private void RequiredNecessary(ref SystemState state)
    {
        state.RequireForUpdate<SetActiveSP>();
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        var ecb = new EntityCommandBuffer(Allocator.TempJob);
        _entityTypeHandle.Update(ref state);
        _linkedBufferLookup.Update(ref state);
        _childBufferLookup.Update(ref state);
        _setActiveSPTypeHandle.Update(ref state);
        var active = new HandleSetActiveJob
        {
            ecb = ecb.AsParallelWriter(),
            linkedGroupBufferLookup = _linkedBufferLookup,
            childBufferLookup = _childBufferLookup,
            entityTypeHandle = _entityTypeHandle,
            setActiveSpTypeHandle = _setActiveSPTypeHandle
        };
        state.Dependency = active.ScheduleParallel(_enQuerySetActive, state.Dependency);
        state.Dependency.Complete();
        ecb.Playback(state.EntityManager);
        ecb.Dispose();
    }


    [BurstCompile]
    partial struct HandleSetActiveJob : IJobChunk
    {
        [WriteOnly] public EntityCommandBuffer.ParallelWriter ecb;
        [ReadOnly] public EntityTypeHandle entityTypeHandle;
        [ReadOnly] public ComponentTypeHandle<SetActiveSP> setActiveSpTypeHandle;
        [ReadOnly] public BufferLookup<LinkedEntityGroup> linkedGroupBufferLookup;
        [ReadOnly] public BufferLookup<Child> childBufferLookup;

        public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask,
            in v128 chunkEnabledMask)
        {
            var setActiveSps = chunk.GetNativeArray(ref setActiveSpTypeHandle);
            var entities = chunk.GetNativeArray(entityTypeHandle);

            for (int i = 0; i < chunk.Count; i++)
            {
                var setActiveSp = setActiveSps[i];
                var entity = entities[i];
                bool stateHandled = HandleState(setActiveSp.state, entity, unfilteredChunkIndex);

                if (stateHandled)
                {
                    ecb.RemoveComponent<SetActiveSP>(unfilteredChunkIndex, entity);
                }
            }
        }

        private bool HandleState(DisableID state, Entity entity, int chunkIndex)
        {
            switch (state)
            {
                case DisableID.Disable:
                    ecb.SetEnabled(chunkIndex, entity, false);
                    return true;
                case DisableID.Enable:
                    if (linkedGroupBufferLookup.HasBuffer(entity))
                    {
                        var buffer = linkedGroupBufferLookup[entity];
                        for (int i = 0; i < buffer.Length; i++)
                        {
                            ecb.RemoveComponent<Disabled>(chunkIndex, buffer[i].Value);
                        }
                    }

                    return true;
                case DisableID.Destroy:
                    ecb.DestroyEntity(chunkIndex, entity);
                    return true;
                case DisableID.DestroyAll:
                    DestroyAllChildren(entity, chunkIndex);
                    return true;
                default:
                    return false;
            }
        }

        private void DestroyAllChildren(Entity entity, int chunkIndex)
        {
            if (childBufferLookup.HasBuffer(entity))
            {
                var buffer = childBufferLookup[entity];
                for (int i = buffer.Length - 1; i >= 0; i--)
                {
                    DestroyAllChildren(buffer[i].Value, chunkIndex);
                }
            }

            ecb.DestroyEntity(chunkIndex, entity);
        }
    }
}

//
[BurstCompile, UpdateInGroup(typeof(PresentationSystemGroup))]
public partial struct HandlePolyCastDamageSystem : ISystem
{
    private EntityManager _entityManager;
    private PhysicsWorldSingleton _physicsWorld;
    
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        Init(ref state);
        RequiredNecessary(ref state);
    }

    [BurstCompile]
    private void RequiredNecessary(ref SystemState state)
    {
        state.RequireForUpdate<LayerStoreComponent>();
        state.RequireForUpdate<PhysicsWorldSingleton>();
    }

    [BurstCompile]
    private void Init(ref SystemState state)
    {
        _entityManager = state.EntityManager;
    }


    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        if(!CheckAndInit(ref state)) return;
        var bufferPolyCastDamageRuntime = SystemAPI.GetSingletonBuffer<BufferPolyCastDamageRuntime>();
        var length = bufferPolyCastDamageRuntime.Length;
        if(length == 0) return;
        var bufferPolyCastDamage = SystemAPI.GetSingletonBuffer<BufferPolyCastDamage>();
        EntityCommandBuffer ecb = new EntityCommandBuffer(Allocator.Temp);
        float time = (float)SystemAPI.Time.ElapsedTime;
        for (var i = length - 1; i >= 0; i--)
        {
            var e = bufferPolyCastDamage[i];
            var eRuntime = bufferPolyCastDamageRuntime[i];
            var isCheckCast = false;
            var isRemove = false;
            
            if(e.startTime + e.delayTime > time) continue;
            
            if (e.applyOnlyOnce)
            {
                isCheckCast = true;
                isRemove = true;
            }
            else if (e.endTime <= time)
            {
                isRemove = true;
            }else if (eRuntime.latestUpdateTime + e.cooldown <= time)
            {
                isCheckCast = true;
            }
            
            
            if (isCheckCast)
            {
                CheckPolyCast(ref state, ref ecb, e);
                eRuntime.latestUpdateTime = time;
                bufferPolyCastDamageRuntime[i] = eRuntime;
            }
            if (isRemove)
            {
                bufferPolyCastDamage.RemoveAtSwapBack(i);
                bufferPolyCastDamageRuntime.RemoveAtSwapBack(i);
            }
        }
        ecb.Playback(_entityManager);
        ecb.Dispose();
    }

    [BurstCompile]
    private bool CheckAndInit(ref SystemState state)
    {
        _physicsWorld = SystemAPI.GetSingleton<PhysicsWorldSingleton>();
        
        return true;
    }

    [BurstCompile]
    private void CheckPolyCast(ref SystemState state, ref EntityCommandBuffer ecb, BufferPolyCastDamage bufferPolyCastDamage)
    {
        var data             = bufferPolyCastDamage.boxCastData;
        var colliderCastHits = new NativeList<ColliderCastHit>(Allocator.Temp);
        if (_physicsWorld.BoxCastAll(data.center, data.orientation, data.halfExtents, data.direct, data.maxDistance,
                ref colliderCastHits, bufferPolyCastDamage.collisionFilter))
        {
            
            foreach (var targetHit in colliderCastHits)
            {
                TakeDamage takeDamage;

                if (_entityManager.HasComponent<TakeDamage>(targetHit.Entity))
                {
                    takeDamage = _entityManager.GetComponentData<TakeDamage>(targetHit.Entity);
                }
                else
                {
                    takeDamage = new()
                    {
                        value = 0
                    };
                }
                takeDamage.value += bufferPolyCastDamage.damage;
                ecb.AddComponent(targetHit.Entity, takeDamage);
            }
        }
        // Debug.Log("m _ Hit : " + colliderCastHits.Length);
        colliderCastHits.Dispose();
    }
} 

//
[UpdateInGroup(typeof(PresentationSystemGroup), OrderLast = true)]
public partial class UpdateHybrid : SystemBase
{

    // Event {
    public delegate void EventPlayerInfo(Vector3 position, Quaternion rotation);
    public delegate void EventCharacterInfo(Quaternion rotation);

    public delegate void EventHitFlashEffect(Vector3 position, Quaternion rotation, EffectID effectID);

    public delegate void EventChangText(TextPropertyEvent textMeshData);

    public delegate void EventItem(NativeArray<BufferSpawnPoint> bufferSpawnPoints, ItemPropertyEvent itemPropertyEvent,EventTypeOnMono type);
    
    public EventPlayerInfo     UpdatePlayerInfo;
    public EventCharacterInfo  UpdateCharacterInfo;
    public EventHitFlashEffect UpdateHitFlashEff;
    public EventChangText      UpdateText;
    public EventItem           ApplyEventItem;
    // Event}

    private Entity _playerEntity;
    
    private LayerStoreComponent _layerStoreComponent;

    private bool _isInit;

    protected override void OnCreate()
    {
        base.OnStartRunning();
        RequireForUpdate<PlayerInfo>();
        RequireForUpdate<LayerStoreComponent>();
    }
    
    protected override void OnUpdate()
    {
        if(!CheckAndInit()) return;
        CallEventPlayer();
        CallEventCharacter();
        UpdateEventPlayOnMono();
    }

    private bool CheckAndInit()
    {

        if (_isInit) return true;
        _isInit = true;
        _playerEntity = SystemAPI.GetSingletonEntity<PlayerInfo>();
        _layerStoreComponent = SystemAPI.GetSingleton<LayerStoreComponent>();
        
        return false;
    }

    private void UpdateEventPlayOnMono()
    {
        var ecb = new EntityCommandBuffer(Allocator.TempJob);
        Entities.ForEach((in EventPlayOnMono eventPlayOnMono,in Entity entity) =>
        {
            var destroyEntity = false;
            switch (eventPlayOnMono.eventTypeOnMono)
            {
                case EventTypeOnMono.ChangeText:
                    HandleEventChangeText(ref ecb, eventPlayOnMono, entity);
                    break;
                case EventTypeOnMono.Effect:
                    HandleEventEffect(ref ecb, eventPlayOnMono, entity);
                    break;
                case EventTypeOnMono.CountersinkItem:
                    destroyEntity = true;
                    HandleEventCountersinkItem(ref ecb,eventPlayOnMono,entity);
                    break;
            }

            if (destroyEntity)
            {
                ecb.DestroyEntity(entity);
            }
            else
            {
                ecb.RemoveComponent<EventPlayOnMono>(entity);
            }
            
        }).WithoutBurst().Run();
        ecb.Playback(EntityManager);
        ecb.Dispose();
    }

    private void HandleEventCountersinkItem(ref EntityCommandBuffer ecb, EventPlayOnMono eventPlayOnMono, Entity entity)
    {
        if(!EntityManager.HasBuffer<BufferSpawnPoint>(entity)) return;
        var property                  = eventPlayOnMono.itemPropertyEvent;
        var bufferSpawnPoint          = EntityManager.GetBuffer<BufferSpawnPoint>(entity);
        var bufferSpawnPointNativeArr = bufferSpawnPoint.ToNativeArray(Allocator.Persistent);
        //
        ApplyEventItem?.Invoke(bufferSpawnPointNativeArr,property,eventPlayOnMono.eventTypeOnMono);
        //
        float xMax = 0;
        float yMax = 0;
        
        foreach (var spawnPoint in bufferSpawnPoint)
        {
            if (yMax < spawnPoint.posInGrid.z)
            {
                yMax = spawnPoint.posInGrid.z;
            }

            if (xMax < spawnPoint.posInGrid.x)
            {
                xMax = spawnPoint.posInGrid.x;
            }
        }
        
        int index1 = 0;
        int index2 = (int)xMax;
        int index3 = (int)((yMax + 1) * (xMax + 1)) - 1;
        
        float with   = math.distance(bufferSpawnPoint[index1].value, bufferSpawnPoint[index2].value) + property.radius * 2f;
        float length = math.distance(bufferSpawnPoint[index2].value, bufferSpawnPoint[index3].value) + property.radius * 2f;
        
        var center      = (bufferSpawnPoint[index1].value + bufferSpawnPoint[index3].value)/2f;
        var halfExtents = new float3(with , property.height , length ) / 2f;
        var startTime   = (float)SystemAPI.Time.ElapsedTime;
        var endTime     = startTime + property.lifeTime;

        DebugDraw.DrawBox(center,halfExtents,property.orentation,float3.zero,Color.green,duration:property.lifeTime);
        
        CollisionFilter filter = new CollisionFilter()
        {
            BelongsTo = _layerStoreComponent.playerLayer,
            CollidesWith = _layerStoreComponent.enemyLayer,
        };
        
        
        BoxCastData boxCastData = new BoxCastData()
        {
            center      = center,
            orientation = property.orentation,
            direct      = float3.zero,
            maxDistance = 0,
            halfExtents = halfExtents,
        };
        
        BufferPolyCastDamage polyCast = new BufferPolyCastDamage()
        {
            type            = PolyCastType.BoxCast,
            startTime       = startTime,
            endTime         = endTime,
            damage          = property.damage,
            boxCastData     = boxCastData,
            collisionFilter = filter,
            cooldown = property.cooldown,
        };

        var bufferPolyCast = SystemAPI.GetSingletonBuffer<BufferPolyCastDamage>();
        var bufferPolyCastRuntime = SystemAPI.GetSingletonBuffer<BufferPolyCastDamageRuntime>();
        bufferPolyCast.Add(polyCast);
        bufferPolyCastRuntime.Add(new()
        {
            latestUpdateTime = startTime,
        });
        bufferSpawnPointNativeArr.Dispose();
    }

    private void HandleEventEffect(ref EntityCommandBuffer ecb, EventPlayOnMono eventPlayOnMono, Entity entity)
    {
        var eff = eventPlayOnMono.effectPropertyEvent;
        UpdateHitFlashEff?.Invoke(eff.position, eff.rotation, eff.effectID);
    }

    private void HandleEventChangeText(ref EntityCommandBuffer ecb, EventPlayOnMono eventPlayOnMono, Entity entity)
    {
        ecb.RemoveComponent<EventPlayOnMono>(entity);
        UpdateText?.Invoke(eventPlayOnMono.textPropertyEvent);
        if (eventPlayOnMono.textPropertyEvent.isRemove)
        {
            ecb.AddComponent(entity, new SetActiveSP()
            {
                state = DisableID.Destroy,
            });
        }
    }
 

    private void CallEventPlayer()
    {
        LocalToWorld ltw = EntityManager.GetComponentData<LocalToWorld>(_playerEntity);
        UpdatePlayerInfo?.Invoke(ltw.Position, ltw.Rotation);
    }

    private void CallEventCharacter()
    {
        var playerInfo = EntityManager.GetComponentData<PlayerInfo>(_playerEntity);
        UpdateCharacterInfo?.Invoke(playerInfo.rotateCharacter);
    }
    
    //JOB
    //JOB

    //
    //
}