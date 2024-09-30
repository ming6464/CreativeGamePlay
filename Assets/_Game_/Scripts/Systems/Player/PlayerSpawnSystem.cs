using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

[BurstCompile, UpdateInGroup(typeof(InitializationSystemGroup))]
public partial struct PlayerSpawnSystem : ISystem
{
    private byte _spawnPlayerState;
    private Entity _characterEntityInstantiate;
    private Entity _entityPlayerInfo;
    private Entity _parentCharacterEntity;
    private bool _spawnInit;
    private EntityManager _entityManager;
    private float2 _spaceGrid;
    private PlayerProperty _playerProperty;
    private int _passCountOfCol;
    private int _passCountCharacter;
    private EntityQuery _enQueryCharacterAlive;
    
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        RequiredNecessary(ref state);
        Init(ref state);
    }

    [BurstCompile]
    private void Init(ref SystemState state)
    {
        _enQueryCharacterAlive =
            SystemAPI.QueryBuilder().WithAll<CharacterInfo,LocalTransform>().WithNone<Disabled, SetActiveSP,AddToBuffer>().Build();
    }

    [BurstCompile]
    private void RequiredNecessary(ref SystemState state)
    {
        state.RequireForUpdate<PlayerProperty>();
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        if(!CheckAndInit(ref state)) return;
        UpdateCharacter(ref state);
        ArrangeCharacter(ref state);
    }
    
    [BurstCompile]
    private bool CheckAndInit(ref SystemState state)
    {
        if (_spawnPlayerState < 2)
        {
            if (_spawnPlayerState == 0)
            {
                EntityCommandBuffer ecb = new EntityCommandBuffer(Allocator.Temp);
                _playerProperty = SystemAPI.GetSingleton<PlayerProperty>();
                var entityPlayer = ecb.CreateEntity();
                ecb.AddComponent(entityPlayer, new PlayerInfo()
                {
                    idWeapon = _playerProperty.idWeaponDefault,
                });
                ecb.AddComponent<LocalToWorld>(entityPlayer);
                ecb.AddComponent(entityPlayer, new LocalTransform()
                {
                    Position = _playerProperty.spawnPosition,
                    Rotation = quaternion.identity,
                    Scale = 1,
                });
                ecb.AddBuffer<BufferCharacterNew>(entityPlayer);
                ecb.AddBuffer<BufferCharacterDie>(entityPlayer);
                var parentCharacter = ecb.CreateEntity();
                ecb.AddComponent<ParentCharacter>(parentCharacter);
                ecb.AddComponent(parentCharacter, new Parent()
                {
                    Value = entityPlayer,
                });
                DotsEX.AddTransformDefault(ref ecb,parentCharacter);
                
                ecb.Playback(state.EntityManager);
                ecb.Dispose();
                _spawnPlayerState = 1;
                return false;
            }
            _parentCharacterEntity = SystemAPI.GetSingletonEntity<ParentCharacter>();
            _entityPlayerInfo = SystemAPI.GetSingletonEntity<PlayerInfo>();
            _characterEntityInstantiate = _playerProperty.characterEntity;
            _entityManager = state.EntityManager;
            _spawnPlayerState = 2;
            _spaceGrid = _playerProperty.spaceGrid;
        }

        return true;
    }


    [BurstCompile]
    private void UpdateCharacter(ref SystemState state)
    {
        EntityCommandBuffer ecb = new EntityCommandBuffer(Allocator.TempJob);
        int spawnChange = 0;
        if (!_spawnInit)
        {
            _spawnInit = true;
            spawnChange = _playerProperty.numberSpawnDefault;
        }

        foreach (var (collection,entity) in SystemAPI.Query<RefRO<ItemCollection>>().WithEntityAccess()
                     .WithNone<Disabled, SetActiveSP>())
        {
            switch (collection.ValueRO.type)
            {
                case ItemType.Character:
                    spawnChange = CalculateNumberPlayer(collection.ValueRO.operation,collection.ValueRO.count);
                    ecb.AddComponent(entity,new SetActiveSP()
                    {
                        state = DisableID.Destroy,
                    });
                    break;
            }
           
        }
        
        Spawn(ref state, ref ecb, spawnChange);
        
        ecb.Playback(_entityManager);
        ecb.Dispose();
        
    }
    
    [BurstCompile]
    private int CalculateNumberPlayer(Operation operation,int count)
    {
        int spawnChange = 0;
        NativeArray<Entity> characterAlive;
        int length = 0;
        switch (operation)
        {
            case Operation.Addition:
                spawnChange += count;
                break;
            case Operation.Subtraction:
                spawnChange -= count;
                break;
            case Operation.Multiplication:
                characterAlive = _enQueryCharacterAlive.ToEntityArray(Allocator.Temp);
                length = characterAlive.Length;
                spawnChange = length;
                characterAlive.Dispose();
                spawnChange *= count;
                spawnChange -= length;
                break;
            case Operation.Division:
                characterAlive = _enQueryCharacterAlive.ToEntityArray(Allocator.Temp);
                length = characterAlive.Length;
                spawnChange = length;
                characterAlive.Dispose();
                spawnChange = (int)math.ceil(spawnChange * 1.0f / count) - length;
                break;
        }

        return spawnChange;
    }
    
    [BurstCompile]
    private void Spawn(ref SystemState state, ref EntityCommandBuffer ecb, int count)
    {
        if(count == 0) return;
        NativeArray<Entity> characterAlive = _enQueryCharacterAlive.ToEntityArray(Allocator.Temp);
        int characterAliveCount = characterAlive.Length;
        var totalNumber = count + characterAliveCount;
        if (totalNumber < 0)
        {
            if (characterAlive.Length == 0)
            {
                characterAlive.Dispose();
                return;
            }
            totalNumber = 0;
        }
        // var bufferDisable = _entityManager.GetBuffer<BufferCharacterDie>(_entityPlayerInfo);
        if (count > 0)
        {
            var lt = DotsEX.LocalTransformDefault();
            // var characterBuffer = _entityManager.GetBuffer<BufferCharacterNew>(_entityPlayerInfo);
            for (int i = characterAliveCount;i < totalNumber; i++)
            {
                Entity entitySet;
                // if (bufferDisable.Length > 0)
                // {
                //     entitySet = bufferDisable[0].entity;
                //     bufferDisable.RemoveAt(0);
                //     ecb.RemoveComponent<Disabled>(entitySet);
                //     ecb.AddComponent(entitySet,new SetActiveSP()
                //     {
                //         state = StateID.Enable,
                //     });
                // }
                // else
                // {
                //     entitySet = _entityManager.Instantiate(_characterEntityInstantiate);
                //     ecb.AddComponent<LocalToWorld>(entitySet);
                // }
                
                entitySet = _entityManager.Instantiate(_characterEntityInstantiate);
                ecb.AddComponent<LocalToWorld>(entitySet);
                
                ecb.AddComponent(entitySet, new CharacterInfo()
                {
                    index = i,
                    hp = _playerProperty.hp,
                });
                ecb.AddComponent(entitySet, new Parent()
                {
                    Value = _parentCharacterEntity,
                });
                ecb.AddComponent(entitySet,lt);
                ecb.AddComponent<New>(entitySet);
                ecb.AddComponent<CanWeapon>(entitySet);
            }
        }
        else
        {
            for (int i = characterAliveCount - 1; i >= totalNumber; i--)
            {
                ecb.AddComponent(characterAlive[i],new TakeDamage()
                {
                    value = 9999,
                });
            }
        }
        characterAlive.Dispose();
    }

    [BurstCompile]
    private void ArrangeCharacter(ref SystemState state)
    {
        EntityCommandBuffer ecb = new EntityCommandBuffer(Allocator.Temp);
        NativeArray<Entity> characterAlive = _enQueryCharacterAlive.ToEntityArray(Allocator.Temp);
        int length = characterAlive.Length;
        if (length == _passCountCharacter)
        {
            characterAlive.Dispose();
            return;
        }
        var characterInfos = _enQueryCharacterAlive.ToComponentDataArray<CharacterInfo>(Allocator.Temp);

        SoftWithIndex(ref characterAlive, ref characterInfos);
        
        int countOfCol = math.max(2,(int)math.ceil(math.sqrt(length)));
        
        for (int i = 0; i < length; i++)
        {
            var characterInfo = characterInfos[i];
            characterInfo.index = i;
            ecb.SetComponent(characterAlive[i],characterInfo);
            ecb.AddComponent(characterAlive[i],new NextPoint()
            {
                value = GetPositionLocal_L(i, countOfCol, _spaceGrid),
            });
        }
        int maxX = length;
        int maxY = 1;
        if (maxX > countOfCol)
        {
            maxX = countOfCol;
            maxY = (int)math.ceil(length * 1.0f / maxX);
        }

        float maxWidthCharacters = _spaceGrid.x * (maxX - 1);
        float maxHeightCharacters = _spaceGrid.y * (maxY - 1);
        ecb.SetComponent(_parentCharacterEntity, new LocalTransform()
        {
            Position = new float3(-maxWidthCharacters / 2f, 0, maxHeightCharacters / 2f),
            Scale = 1,
            Rotation = quaternion.identity,
        });

        
        var playInfo = SystemAPI.GetComponentRW<PlayerInfo>(_entityPlayerInfo);
        playInfo.ValueRW.maxXGridCharacter = maxX;
        playInfo.ValueRW.maxYGridCharacter = maxY;
        _passCountCharacter = length;
        characterAlive.Dispose();
        ecb.Playback(_entityManager);
        ecb.Dispose();

        #region Local Func

        float3 GetPositionLocal_L(int index, int maxCol, float2 space)
        {
            float3 grid = GetGridPos_L(index, maxCol);
            grid.z *= -space.y;
            grid.x *= space.x;

            return grid;
        }
        
        float3 GetGridPos_L(int index, int maxCol)
        {
            var grid = new float3(0, 0, 0);

            if (index < 0)
            {
                grid.x = -1;
            }
            else if (index < countOfCol)
            {
                grid.x = index;
            }
            else
            {
                grid.x = index % maxCol;
                grid.z = index / maxCol;
            }
            
            return grid;
        }

        #endregion
        
    }

    [BurstCompile]
    private void SoftWithIndex(ref NativeArray<Entity> characterAlive, ref NativeArray<CharacterInfo> characterInfos)
    {
        var length = characterAlive.Length;
        for (int i = 0; i < length - 1; i++)
        {
            for (int j = i + 1; j < length; j++)
            {
                if (characterInfos[j].index < characterInfos[i].index)
                {
                    (characterAlive[i], characterAlive[j]) = (characterAlive[j], characterAlive[i]);
                    (characterInfos[i], characterInfos[j]) = (characterInfos[j], characterInfos[i]);
                }
            }
        }
    }
}
