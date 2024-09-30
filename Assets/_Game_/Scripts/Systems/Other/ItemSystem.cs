 using _Game_.Scripts.Data;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace _Game_.Scripts.Systems.Other
{
    [BurstCompile,UpdateInGroup(typeof(InitializationSystemGroup))]
    public partial struct ItemSystem : ISystem
    {
        private NativeArray<BufferObstacle> _buffetObstacle;
        private EntityManager _entityManager;
        private bool _isInit;
        private float _time;

        #region OnCreate

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            RequireNecessaryComponents(ref state);
        }
        [BurstCompile]
        private void RequireNecessaryComponents(ref SystemState state)
        {
            state.RequireForUpdate<PlayerInfo>();
        }

        #endregion
        

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {
            if (_buffetObstacle.IsCreated)
                _buffetObstacle.Dispose();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            if(!CheckAndInit(ref state)) return;
            _time += SystemAPI.Time.DeltaTime;
            CheckObstacleItem(ref state);
            CheckItemShooting(ref state);
        }

        [BurstCompile]
        private bool CheckAndInit(ref SystemState state)
        {
            if (_isInit) return true;
            _isInit = true;
            _entityManager = state.EntityManager;
            _buffetObstacle = SystemAPI.GetSingletonBuffer<BufferObstacle>().ToNativeArray(Allocator.Persistent);
            _time = (float)SystemAPI.Time.ElapsedTime;
            return false;
        }

        [BurstCompile]
        private void CheckItemShooting(ref SystemState state)
        {
            EntityCommandBuffer ecb = new EntityCommandBuffer(Allocator.Temp);
            var time = (float)SystemAPI.Time.ElapsedTime;
            float timeEffect = 0.1f;

            ResetHit(ref state, ref ecb,time,timeEffect);
            
            var hitCheckTime = new HitCheckTime()
            {
                time = time,
            };
            
            foreach (var (itemInfo, takeDamage,hitCheckOverride, entity) in SystemAPI.Query<RefRW<ItemInfo>, RefRO<TakeDamage>,RefRW<HitCheckOverride>>()
                         .WithEntityAccess().WithNone<Disabled,AddToBuffer>())
            {
                itemInfo.ValueRW.hp -= (int)takeDamage.ValueRO.value;
                ecb.RemoveComponent<TakeDamage>(entity);


                TextPropertyEvent textPropertyEvent = new TextPropertyEvent()
                {
                    id = itemInfo.ValueRO.idTextHp,
                    number = itemInfo.ValueRO.hp,
                    isRemove = itemInfo.ValueRO.hp <= 0,
                };
                
                ecb.AddComponent(entity,new EventPlayOnMono()
                {
                    eventTypeOnMono = EventTypeOnMono.ChangeText,
                    textPropertyEvent = textPropertyEvent
                });
                if (itemInfo.ValueRO.hp <= 0)
                {
                    HandleHpZero(ref state, ref ecb, itemInfo.ValueRO, entity);
                }
                else
                {
                    if (_entityManager.HasComponent<HitCheckTime>(entity))
                    {
                        var hitCheck = _entityManager.GetComponentData<HitCheckTime>(entity);
                        if(time - hitCheck.time < timeEffect) continue;
                    }
                    var value = hitCheckOverride.ValueRW.Value;
                    value += 1;
                    if (value > 2)
                    {
                        value = 1;
                    }

                    if (value - 1 == 0)
                    {
                        hitCheckTime.time -= timeEffect / 2f;
                    }
                    ecb.AddComponent(entity,hitCheckTime);
                    hitCheckOverride.ValueRW.Value = value;
                }
            }
            
            ecb.Playback(_entityManager);
            ecb.Dispose();
        }
        
        [BurstCompile]
        private void HandleHpZero(ref SystemState state, ref EntityCommandBuffer ecb, ItemInfo itemInfo, Entity entity)
        {
            var entityNew = ecb.CreateEntity();
            var entityChangeText = ecb.CreateEntity();
            var lt = _entityManager.GetComponentData<LocalTransform>(entity);
            ecb.AddComponent(entityNew,lt);
            
            var text = new TextPropertyEvent
            {
                id = itemInfo.idTextHp,
                number = 0,
            };
            
            ecb.AddComponent(entityChangeText, new EventPlayOnMono()
            {
                eventTypeOnMono = EventTypeOnMono.ChangeText,
                textPropertyEvent = text,
            });
            
            if (_entityManager.HasBuffer<BufferSpawnPoint>(entity))
            {
                var buffer = ecb.AddBuffer<BufferSpawnPoint>(entityNew);
                buffer.CopyFrom(_entityManager.GetBuffer<BufferSpawnPoint>(entity));
            }
            
            ecb.AddComponent(entityNew,new ItemCollection()
            {
                count      = itemInfo.count,
                entityItem = entityNew,
                id         = itemInfo.id,
                type       = itemInfo.type,
                operation  = itemInfo.operation,
            });
            
            ecb.AddComponent(entity,new SetActiveSP()
            {
                state = DisableID.DestroyAll
            });
        }

        [BurstCompile]
        private void ResetHit(ref SystemState state, ref EntityCommandBuffer ecb, float time,float timeEffect)
        {
            foreach (var (hitCheckOverride, hitTime, entity) in SystemAPI
                         .Query<RefRW<HitCheckOverride>, RefRW<HitCheckTime>>().WithEntityAccess().WithAll<ItemInfo>().WithNone<Disabled,TakeDamage>())
            {
                if(time - hitTime.ValueRO.time < timeEffect) continue;
                hitCheckOverride.ValueRW.Value -= 1;

                if (hitCheckOverride.ValueRO.Value > 0)
                {
                    hitTime.ValueRW.time = time - timeEffect / 2f;
                }
                else
                {
                    ecb.RemoveComponent<HitCheckTime>(entity);
                }
            }
        }

        [BurstCompile]
        private void CheckObstacleItem(ref SystemState state)
        {
            EntityCommandBuffer ecb = new EntityCommandBuffer(Allocator.Temp);
            foreach (var (collection,entity) in SystemAPI.Query<RefRO<ItemCollection>>().WithEntityAccess()
                         .WithNone<Disabled, SetActiveSP>())
            {
                var disable = false;
                
                switch (collection.ValueRO.type)
                {
                    case ItemType.ObstacleTurret:
                        SpawnTurret(ref state,ref ecb,collection.ValueRO);
                        disable = true;
                        break;
                    case ItemType.Obstacle:
                        SpawnObstacle(ref state, ref ecb, collection.ValueRO);
                        disable = true;
                        break;
                    case ItemType.ObstacleCounterSink:
                        SpawnObstacleCounterSink(ref state, ref ecb, collection.ValueRO);
                        disable = true;
                        break;
                }

                if (disable)
                {
                    ecb.AddComponent(entity,new SetActiveSP()
                    {
                        state = DisableID.Disable
                    });
                }
            }
            ecb.Playback(_entityManager);
            ecb.Dispose();
        }

        private void SpawnObstacleCounterSink(ref SystemState state, ref EntityCommandBuffer ecb, ItemCollection itemCollection)
        {
            var obstacle = GetObstacle(itemCollection.id);
            
            if(obstacle.id < 0) return;
            
            var points = _entityManager.GetBuffer<BufferSpawnPoint>(itemCollection.entityItem);
            var lt     = _entityManager.GetComponentData<LocalTransform>(itemCollection.entityItem);
            var entity = ecb.CreateEntity();
            var buffer = ecb.AddBuffer<BufferSpawnPoint>(entity);
            buffer.CopyFrom(points);
            points.Clear();//
            
            var counterSink = obstacle.counterSinkObstacle;
            var itemPropertyEvent = new ItemPropertyEvent()
            {
                    radius    = counterSink.radius,
                    height    = counterSink.height,
                    lifeTime  = counterSink.lifeTime,
                    damage    = counterSink.damage,
                    delayTime = counterSink.delayTime,
                    orentation = lt.Rotation,
                    cooldown = counterSink.cooldown,
            };
            
            ecb.AddComponent(entity,new EventPlayOnMono
            {
                    eventTypeOnMono   = EventTypeOnMono.CountersinkItem,
                    itemPropertyEvent = itemPropertyEvent,
            });
        }

        [BurstCompile]
        private void SpawnObstacle(ref SystemState state, ref EntityCommandBuffer ecb, ItemCollection itemCollection)
        {
            var obstacle = GetObstacle(itemCollection.id);
            if(obstacle.id < 0) return;
            var points = _entityManager.GetBuffer<BufferSpawnPoint>(itemCollection.entityItem);
            var obs    = ecb.Instantiate(obstacle.entity);
            var buffer = ecb.AddBuffer<BufferSpawnPoint>(obs);
            buffer.CopyFrom(points);
            points.Clear();//
        }

        [BurstCompile]
        private BufferObstacle GetObstacle(int id)
        {
            foreach (var i in _buffetObstacle)
            {
                if (i.id == id) return i;
            }

            return new BufferObstacle()
            {
                id = -1,
            };
        }
        
        [BurstCompile]
        private void SpawnTurret(ref SystemState state,ref EntityCommandBuffer ecb,ItemCollection itemCollection)
        {
            var obstacle = GetObstacle(itemCollection.id);
            if(obstacle.id < 0) return;
            var turret = obstacle.turretObstacle;
            var points = _entityManager.GetBuffer<BufferSpawnPoint>(itemCollection.entityItem);
            LocalTransform lt = new LocalTransform()
            {
                Scale = 1,
                Rotation = quaternion.identity
            };
            foreach (var point in points)
            {
                var newObs = ecb.Instantiate(obstacle.entity);
                lt.Position = point.value;
                ecb.AddComponent(newObs,lt);
                ecb.AddComponent(newObs,new TurretInfo()
                {
                    id = itemCollection.id,
                    timeLife = turret.timeLife,
                    startTime = _time,
                });
            }
            points.Clear();
        }
    }
}

