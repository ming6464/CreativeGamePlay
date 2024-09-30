using System;
using _Game_.Scripts.Data;
using Unity.Entities;
using UnityEngine;

namespace _Game_.Scripts.AuthoringAndMono
{
    public class DataAuthoring : MonoBehaviour
    {
        [Tooltip("Dữ liệu weapon")]
        public WeaponSO weaponSo;
        [Tooltip("Dữ liệu zombie")]
        public ZombieSO zombieSo;
        [Tooltip("Dữ liệu các chướng ngại vật")]
        public ObstacleSO obstacleSo;
        private class DataAuthoringBaker : Baker<DataAuthoring>
        {
            public override void Bake(DataAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.None);
                AddComponent<DataProperty>(entity);
                var weaponBuffer = AddBuffer<BufferWeaponStore>(entity);
                var zombieBuffer = AddBuffer<BufferZombieStore>(entity);
                var obstacleBuffer = AddBuffer<BufferObstacle>(entity);
                
                // add Buffer weapon
                foreach (var weapon in authoring.weaponSo.weapons)
                {
                    weaponBuffer.Add(new BufferWeaponStore()
                    {
                        id = weapon.id,
                        entity = GetEntity(weapon.weaponPrefab,TransformUsageFlags.None),
                        offset = weapon.offset,
                        damage = weapon.damage,
                        speed = weapon.speed,
                        cooldown = weapon.cooldown,
                        bulletPerShot = weapon.bulletPerShot,
                        spaceAnglePerBullet = weapon.spaceAnglePerBullet,
                        parallelOrbit = weapon.parallelOrbit,
                        spacePerBullet = weapon.spacePerBullet,
                    });
                }
                //

                // Add buffer zombie
                foreach (var zombie in authoring.zombieSo.zombies)
                {
                    zombieBuffer.Add(new BufferZombieStore()
                    {
                        priorityKey = zombie.priorityKey,
                        id = zombie.id,
                        entity = GetEntity(zombie.prefab,TransformUsageFlags.Dynamic),
                        hp = zombie.hp,
                        radius = zombie.radius,
                        speed = zombie.speed,
                        damage = zombie.damage,
                        attackRange = zombie.attackRange,
                        delayAttack = zombie.delayAttack,
                        chasingRange = zombie.chasingRange,
                        radiusDamage = zombie.radiusDamage,
                        offsetAttackPosition = zombie.offsetAttackPosition,
                    });
                }
                //
                
                // Add buffer obstacle
                foreach (var obs in authoring.obstacleSo.obstacles)
                {
                    switch (obs.obstacle.type)
                    {
                        case ItemType.ObstacleTurret:
                            var turret = (TurrentSO) obs.obstacle;

                            var turretData = new TurretData()
                            {
                                bulletPerShot = turret.bulletPerShot,
                                distanceAim = turret.distanceAim,
                                moveToWardMax = turret.moveToWardMax,
                                moveToWardMin = turret.moveToWardMin,
                                parallelOrbit = turret.parallelOrbit,
                                pivotFireOffset = turret.pivotFireOffset,
                                spaceAnglePerBullet = turret.spaceAnglePerBullet,
                                spacePerBullet = turret.spacePerBullet,
                                timeLife = turret.timeLife,
                            };
                            
                            obstacleBuffer.Add(new BufferObstacle()
                                {
                                    turretObstacle = turretData,
                                    id = obs.id,
                                    entity = GetEntity(turret.prefabs,TransformUsageFlags.None),
                                    cooldown = turret.cooldown,
                                    damage = turret.damage,
                                    speed = turret.speed,
                                }
                            );
                            break;
                        case ItemType.ObstacleCounterSink: 
                            var countersink = (CountersinkSO) obs.obstacle;
                            var countersinkData = new CounterSinkData()
                            {
                                damage = countersink.damage,
                                delayTime = countersink.delayTime,
                                height = countersink.height,
                                lifeTime = countersink.lifeTime,
                                radius = countersink.radius,
                                cooldown = countersink.cooldown,
                            };
                            obstacleBuffer.Add(new BufferObstacle()
                            {
                                counterSinkObstacle = countersinkData,
                                id = obs.id,
                            });
                            break;
                        case ItemType.Obstacle:
                            obstacleBuffer.Add(new BufferObstacle()
                            {
                                    entity = GetEntity(obs.obstacle.prefabs, TransformUsageFlags.None),
                                    id = obs.id,
                            });
                            break;
                    }
                }
                //
            }
        }
    }
}
