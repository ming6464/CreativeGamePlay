using System;
using _Game_.Scripts.Data;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;
using UnityEngine;


public struct ItemProperty : IComponentData
{
    public Entity entity;
}

public struct ItemInfo : IComponentData
{
    public int id;
    public ItemType type;
    public int count;
    public int hp;
    public Operation operation;
    public int idTextHp;
}

public struct ItemCollection : IComponentData
{
    public ItemType type;
    public Entity entityItem;
    public int count;
    public int id;
    public Operation operation;
}

[Serializable]
public struct BufferSpawnPoint : IBufferElementData
{
    public float3 value;
    public float3 posInGrid;
}

public struct ItemCanShoot : IComponentData
{
    
}

//obstacle

public struct BufferObstacle : IBufferElementData
{
    public int             id;
    public Entity          entity;
    public float           speed;
    public float           damage;
    public float           cooldown;
    public TurretData      turretObstacle;
    public CounterSinkData counterSinkObstacle;
}

public struct CounterSinkData
{
    public float lifeTime;
    public float radius;
    public float height;
    public float damage;
    public float delayTime;
    public float cooldown;
}

public struct TurretData
{
    public float3 pivotFireOffset;
    public int bulletPerShot;
    public float spaceAnglePerBullet;
    public float spacePerBullet;
    public bool parallelOrbit;
    public float timeLife;
    public float distanceAim;
    public float moveToWardMax;
    public float moveToWardMin;
}

public struct EventPlayOnMono : IComponentData
{
    public EventTypeOnMono eventTypeOnMono;
    public ItemPropertyEvent itemPropertyEvent;
    public EffectPropertyEvent effectPropertyEvent;
    public TextPropertyEvent textPropertyEvent;
}

public struct ItemPropertyEvent
{
    public float      lifeTime;
    public float      radius;
    public float      height;
    public float      damage;
    public float      delayTime;
    public float      cooldown;
    public quaternion orentation;
}

public struct EffectPropertyEvent
{
    public EffectID effectID;
    public float3 position;
    public quaternion rotation;
}

public struct TextPropertyEvent
{
    public int id;
    public FixedString32Bytes text;
    public int number;
    public Vector3 position;
    public Vector3 offset;
    public bool textFollowPlayer;
    public bool isRemove;
}

public struct TurretInfo : IComponentData
{
    public int id;
    public float timeLife;
    public float startTime;
}

public struct BarrelRunTime : IComponentData
{
    public float value;
}

public struct BarrelInfo : IComponentData
{
    public float speed;
    public float damage;
    public float cooldown;
    public float distanceAim;
    public float moveToWardMax;
    public float moveToWardMin;
    public float3 pivotFireOffset;
    public int bulletPerShot;
    public float spaceAnglePerBullet;
    public float spacePerBullet;
    public bool parallelOrbit;
}

public struct BarrelCanSetup : IComponentData
{
    public int id;
}

[MaterialProperty("_isHit")]
public struct HitCheckOverride : IComponentData
{
    public float Value;
}

public struct HitCheckTime : IComponentData
{
    public float time;
}

//Enum
public enum ItemType
{
    Character,
    Weapon,
    ObstacleTurret,
    Obstacle,
    ObstacleCounterSink,
}

public enum EventTypeOnMono
{
    CountersinkItem,
    ChangeText,
    Effect
}