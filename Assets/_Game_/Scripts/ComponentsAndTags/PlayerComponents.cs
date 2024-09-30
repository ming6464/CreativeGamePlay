using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

public struct PlayerProperty : IComponentData
{
    public float   hp;
    public int     idWeaponDefault;
    public Entity  characterEntity;
    public float   characterRadius;
    public float   speed;
    public AimType aimType;
    public float3  spawnPosition;
    public int     numberSpawnDefault;
    public float2  spaceGrid;
    public float   distanceAim;
    public float   moveToWardMax;
    public float   moveToWardMin;
    public float   speedMoveToNextPoint;
    public bool    aimNearestEnemy;
    public bool    helicopterMode;
    
}

public struct PlayerInfo : IComponentData
{
    public int        idWeapon;
    public int        maxXGridCharacter;
    public int        maxYGridCharacter;
    public quaternion rotateCharacter;
}


public readonly partial struct PlayerAspect : IAspect
{
    public readonly Entity entity;
    private readonly RefRW<LocalTransform> _localTransform;
    private readonly RefRO<PlayerInfo> _playerInfo;
    private readonly RefRO<LocalToWorld> _localToWorld;

    public float3 Position
    {
        get => _localTransform.ValueRO.Position;
        set => _localTransform.ValueRW.Position = value;
    }

    public quaternion Rotation
    {
        get => _localTransform.ValueRO.Rotation;
        set => _localTransform.ValueRW.Rotation = value;
    }

    public float Scale
    {
        get => _localTransform.ValueRO.Scale;
        set => _localTransform.ValueRW.Scale = value;
    }
    
    public float3 PositionWorld => _localToWorld.ValueRO.Position;

    public quaternion RotationWorld => new quaternion(_localToWorld.ValueRO.Value);

    public float ScaleWorld => math.length(_localToWorld.ValueRO.Value.c0.xyz); // Assuming uniform scale

    public LocalTransform LocalTransform => _localTransform.ValueRO;
    public LocalToWorld LocalToWorld => _localToWorld.ValueRO;
}


public struct PlayerInput: IComponentData
{
    public bool       pullTrigger;
    public float2     directMove;
    public float3     directMouse;
    public float3     mousePosition;
    public quaternion rotationOnHelicopterMode;
    public bool       rota3D;
}


public struct MoveAuto
{
    
}

public struct bufferMoveDestination : IBufferElementData
{
    public float3 position;
    public float speed;
}


// Character

public struct BufferCharacterNew : IBufferElementData
{
    public Entity entity;
}

public struct BufferCharacterDie : IBufferElementData
{
    public Entity entity;
}

public struct ParentCharacter : IComponentData
{
    
}

public struct CharacterInfo : IComponentData
{
    public int index;
    public Entity weaponEntity;
    public float hp;
}

public struct NextPoint : IComponentData
{
    public float3 value;
}

public readonly partial struct CharacterAspect : IAspect
{
    public readonly Entity entity;
    private readonly RefRW<LocalTransform> _localTransform;
    private readonly RefRO<CharacterInfo> _characterInfo;
    private readonly RefRO<LocalToWorld> _localToWorld;

    public float3 Position
    {
        get => _localTransform.ValueRO.Position;
        set => _localTransform.ValueRW.Position = value;
    }

    public quaternion Rotation
    {
        get => _localTransform.ValueRO.Rotation;
        set => _localTransform.ValueRW.Rotation = value;
    }

    public float Scale
    {
        get => _localTransform.ValueRO.Scale;
        set => _localTransform.ValueRW.Scale = value;
    }
    
    public float3 PositionWorld => _localToWorld.ValueRO.Position;

    public quaternion RotationWorld => new quaternion(_localToWorld.ValueRO.Value);

    public float ScaleWorld => math.length(_localToWorld.ValueRO.Value.c0.xyz); // Assuming uniform scale

    public LocalTransform LocalTransform => _localTransform.ValueRO;
    public LocalToWorld LocalToWorld => _localToWorld.ValueRO;
}