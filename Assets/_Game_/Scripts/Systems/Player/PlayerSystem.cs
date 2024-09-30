using _Game_.Scripts.Systems.Player;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;
using UnityEngine;

[UpdateInGroup(typeof(SimulationSystemGroup)),UpdateAfter(typeof(CharacterSystem))]
public partial struct PlayerSystem : ISystem
{
    private EntityManager _entityManager;
    private PlayerProperty _playerProperty;
    private PlayerInput _playerMoveInput;
    private PlayerInfo _playerInfo;
    private Entity _playerEntity;
    private bool _init;
    private PlayerAspect _playerAspect;
    //
    private int _maxXGridCharacter;
    private int _maxYGridCharacter;
    private float2 _spaceGrid;
    private float _characterRadius;
    private LocalToWorld _ltwPlayer;
    private PhysicsWorldSingleton _physicsWorld;
    private NativeList<ColliderCastHit> _itemColliders;
    private CollisionFilter _filterItem;
    private LayerStoreComponent _layerStore;
    private EntityQuery _entityQuery;
    private CollisionFilter _collisionFilter;
    
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        RequireNecessaryComponents(ref state);
        _itemColliders = new(Allocator.Persistent);
        _entityQuery   = SystemAPI.QueryBuilder().WithAll<CharacterInfo>().WithNone<Disabled,AddToBuffer>().Build();
    }
    
    [BurstCompile]
    public void OnDestroy(ref SystemState state)
    {
        if (_itemColliders.IsCreated)
            _itemColliders.Dispose();
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        if(!CheckAndInit(ref state)) return;
        UpdateFieldRunTime(ref state);
        Move(ref state);
        CheckCollider(ref state);
    }

    [BurstCompile]
    private void Move(ref SystemState state)
    {
        if(_entityQuery.IsEmpty) return;
        _playerAspect = SystemAPI.GetAspect<PlayerAspect>(_playerEntity);
        float2 direct = _playerMoveInput.directMove;
        if(math.length(direct) > 1)
            direct = math.normalize(direct);
        _playerAspect.Position += new float3(direct.x, 0, direct.y) * _playerProperty.speed * SystemAPI.Time.DeltaTime;
    }
    [BurstCompile]
    private void CheckCollider(ref SystemState state)
    {
        EntityCommandBuffer ecb = new EntityCommandBuffer(Allocator.Temp);
        var halfSizeBox = GetHalfSizeBoxPlayer(ref state);
        _itemColliders.Clear();
        if (_physicsWorld.BoxCastAll(_ltwPlayer.Position, quaternion.identity, halfSizeBox, float3.zero, 0,
                ref _itemColliders, _filterItem))
        {
            HandleItemCollider(ref state, ref ecb, _itemColliders);
        }
        ecb.Playback(_entityManager);
    }
    [BurstCompile]
    private void HandleItemCollider(ref SystemState state,ref EntityCommandBuffer ecb, NativeList<ColliderCastHit> arrItem)
    {
        foreach (var t in arrItem)
        {
            Entity entityItem = t.Entity;
                
            if(!_entityManager.HasComponent<ItemInfo>(entityItem))continue;
                
            var itemInfo = _entityManager.GetComponentData<ItemInfo>(entityItem);

            var entityCollectionNew = _entityManager.CreateEntity();

            if (_entityManager.HasBuffer<BufferSpawnPoint>(entityItem))
            {
                var buffer = ecb.AddBuffer<BufferSpawnPoint>(entityCollectionNew);
                buffer.CopyFrom(_entityManager.GetBuffer<BufferSpawnPoint>(entityItem));
            }
                
            ecb.AddComponent(entityCollectionNew,new ItemCollection()
            {
                type = itemInfo.type,
                count = itemInfo.count,
                id = itemInfo.id,
                entityItem = entityCollectionNew,
                operation = itemInfo.operation,
            });
            ecb.AddComponent(entityItem,new SetActiveSP()
            {
                state = DisableID.DestroyAll,
            });
        }
    }
    [BurstCompile]
    private float3 GetHalfSizeBoxPlayer(ref SystemState state)
    {
        var halfX = (_maxXGridCharacter - 1) * _spaceGrid.x / 2f + _characterRadius;
        var halfZ = (_maxYGridCharacter - 1) * _spaceGrid.y / 2f + _characterRadius;
        return new float3(halfX, 1, halfZ);
    }
    [BurstCompile]
    private void RequireNecessaryComponents(ref SystemState state)
    {
        state.RequireForUpdate<LayerStoreComponent>();
        state.RequireForUpdate<PhysicsWorldSingleton>();
        state.RequireForUpdate<PlayerProperty>();
        state.RequireForUpdate<PlayerInput>();
        state.RequireForUpdate<PlayerInfo>();
        state.RequireForUpdate<CharacterInfo>();
    }
    [BurstCompile]
    private bool CheckAndInit(ref SystemState state)
    {
        if (!_init)
        {
            _init = true;
            _playerProperty = SystemAPI.GetSingleton<PlayerProperty>();
            _playerEntity = SystemAPI.GetSingletonEntity<PlayerInfo>();
            _characterRadius = _playerProperty.characterRadius;
            _layerStore = SystemAPI.GetSingleton<LayerStoreComponent>();
            _filterItem = new CollisionFilter()
            {
                BelongsTo = _layerStore.playerLayer,
                CollidesWith = _layerStore.itemLayer,
                GroupIndex = 0,
            };
            _spaceGrid = _playerProperty.spaceGrid;
            _characterRadius = _playerProperty.characterRadius;
            _entityManager = state.EntityManager;
            return false;
        }
        _playerMoveInput = SystemAPI.GetSingleton<PlayerInput>();
        return true;
    }
    [BurstCompile]
    private void UpdateFieldRunTime(ref SystemState state)
    {
        _physicsWorld = SystemAPI.GetSingleton<PhysicsWorldSingleton>();
        _playerInfo = SystemAPI.GetSingleton<PlayerInfo>();
        _maxXGridCharacter = math.max(1,_playerInfo.maxXGridCharacter);
        _maxYGridCharacter = math.max(1,_playerInfo.maxYGridCharacter);
        _ltwPlayer = SystemAPI.GetComponentRO<LocalToWorld>(_playerEntity).ValueRO;
    }
}

public enum AimType
{
    TeamAim,
    IndividualAim
}
