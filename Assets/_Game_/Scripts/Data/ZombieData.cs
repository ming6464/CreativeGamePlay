using System;
using Unity.Mathematics;
using UnityEngine;


[CreateAssetMenu(menuName = "DataSO/ZombieSO")]
public class ZombieSO : ScriptableObject
{
    public Zombie[] zombies;
}

[Serializable]
public struct Zombie
{
    public PriorityKey priorityKey;
    public int id;
    public GameObject prefab;
    public float hp;
    public float radius;
    public float speed;
    public float damage;
    public float attackRange;
    public float delayAttack;
    public float chasingRange;
    public float radiusDamage;
    public float3 offsetAttackPosition;
}

