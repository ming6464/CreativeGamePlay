using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

public struct WeaponProperty : IComponentData
{
    public bool shootAuto;
    //
    public Entity entityBullet;
    public float length;
    //
    public float timeLife;
    public float2 damageRangeRatio;
    public float2 timeRange;
}

public struct WeaponInfo : IComponentData
{
    public int id;
}

public struct BufferWeaponStore : IBufferElementData
{
    public int id;
    public Entity entity;
    public float3 offset;
    public float damage;
    public float speed;
    public float cooldown;
    public int bulletPerShot;
    public float spacePerBullet;
    public float spaceAnglePerBullet;
    public bool parallelOrbit;
}

public readonly partial struct WeaponAspect : IAspect
{
    public readonly Entity entity;
    private readonly RefRO<WeaponInfo> _weaponInfo;
    private readonly RefRW<LocalTransform> _localTransform;
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
}

//Bullet
public struct BulletInfo : IComponentData
{
    public float startTime;
    public float damage;
    public float speed;
}

public struct BufferBulletSpawner : IBufferElementData
{
    public float damage;
    public float speed;
    public LocalTransform lt;
    public float bulletPerShot;
    public float spaceAnglePerBullet;
    public float spacePerBullet;
    public float3 right;
    public bool parallelOrbit;
}

public readonly partial struct BulletAspect : IAspect
{
    public readonly Entity entity;
    private readonly RefRO<BulletInfo> _bulletInfo;
    private readonly RefRW<LocalTransform> _localTransform;

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

    public LocalTransform LocalTransform => _localTransform.ValueRO;
}

public partial struct BufferBulletDisable : IBufferElementData
{
    public Entity entity;
}