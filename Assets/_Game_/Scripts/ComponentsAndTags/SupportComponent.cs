using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;


public struct LayerStoreComponent : IComponentData
{
    public uint defaultLayer;
    public uint playerLayer;
    public uint characterLayer;
    public uint enemyLayer;
    public uint enemyDieLayer;
    public uint bulletLayer;
    public uint itemLayer;
    public uint itemCanShootLayer;
}


public struct SetActiveSP : IComponentData
{
    public DisableID state;
}


public struct SetAnimationSP : IComponentData
{
    public StateID state;
    public float timeDelay;
}

//Enum

public enum EffectID
{
    HitFlash = 0,
    GroundCrack = 1,
    MetalImpact = 2,
}

public enum DisableID
{
    Disable,
    Enable,
    DisableAll,
    EnableAll,
    Destroy,
    DestroyAll,
}


public enum StateID
{
    None,
    Die,
    WaitToPool,
    Enable,
    Run,
    Attack,
    Idle,
    WaitRemove
}

public enum CameraType
{
    FirstPersonCamera,
    ThirstPersonCamera,
    HelicopterMode
}

public enum PolyCastType
{
    BoxCast,
}

//Enum

//Events {

//Events }

//other components

public struct AddToBuffer : IComponentData
{
    public int id;
    public Entity entity;
}

public struct TakeDamage : IComponentData
{
    public float value;
}

public struct NotUnique : IComponentData
{
}

public struct New : IComponentData
{
    
}

public struct CanWeapon : IComponentData
{
    
}

public struct DataProperty : IComponentData
{
    
}

public struct CameraMode : IComponentData
{
    public CameraType cameraType;
}

//Buffer{
public struct BufferPolyCastDamage : IBufferElementData
{
    public PolyCastType type;
    public bool applyOnlyOnce;
    public float startTime;
    public float endTime;
    public float delayTime;
    public float cooldown;
    public float damage;
    public CollisionFilter collisionFilter;
    public BoxCastData boxCastData;
}

public struct BufferPolyCastDamageRuntime : IBufferElementData
{
    public float latestUpdateTime;
}

public struct BoxCastData
{
    public float3 center;
    public quaternion orientation;
    public float3 halfExtents;
    public float3 direct;
    public float maxDistance;
}

public struct BufferTakeDamage : IBufferElementData
{
    public float value;
}
//Buffer }
//