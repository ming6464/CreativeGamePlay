using System;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

public class ZombieAuthoring : MonoBehaviour
{
    [Header("Spawn Zombie Normal")] [Tooltip("ID của các zombie thường sẽ được spawn")]
    public int[] zombieNormalIds;

    [Tooltip("Các vùng zombie thường sẽ spawn ngẫu nhiên")]
    public SpawnRange[] spawnRanges;

    [Header("Spawn Zombie Boss")] [Tooltip("Thông tin về các zombie boss sẽ được spawn")]
    public SpawnBoss[] spawnBosses;

    [Header("Spawn Info")] [Tooltip("Tổng số zombie thường được spawn (không tính boss)")]
    public int totalNumber;

    [Tooltip("Thời gian chờ giữa các lần spawn zombie")]
    public float cooldown;

    [Tooltip("Phạm vi số lượng zombie được spawn mỗi lần")]
    public float2 spawnAmountRange;

    [Tooltip("Phạm vi thời gian để điều chỉnh số lượng spawn mỗi lần\n\nví dụ:\nspawnAmountRange = (10, 100)\ntimeRange = (1, 10)\n\nThời gian tính từ lúc chạy game là time\nSố lượng mỗi lần spawn là count\n\nnếu time <= 1 thì count = 10\nnếu time >= 10 thì count = 100\ncòn lại count sẽ nằm trong khoảng 10 -> 100 dựa theo time")]
    public float2 timeRange;

    [Tooltip("Có spawn zombie vô hạn hay không")]
    public bool spawnInfinity;

    [Tooltip("Có cho phép zombie respawn sau khi chết hay không")]
    public bool allowRespawn;

    [Tooltip("Có cho phép spawn zombie thường hay không")]
    public bool allowSpawnZombie;

    [Tooltip("Có cho phép spawn zombie boss hay không")]
    public bool allowSpawnBoss;

    [Header("General Properties")] [Tooltip("Tốc độ tránh của zombie")]
    public float speedAvoid;

    [Tooltip("So sánh ưu tiên của zombie trong hành vi nhất định")]
    public bool comparePriorities;

    [Tooltip("Khoảng cách tối thiểu để zombie tránh vật thể")]
    public float minDistanceAvoid;


    class ZombieBaker : Baker<ZombieAuthoring>
    {
        public override void Bake(ZombieAuthoring authoring)
        {
            Entity entity = GetEntity(TransformUsageFlags.None);
            AddBuffer<BufferZombieDie>(entity);
            AddComponent<ZombieSpawnRuntime>(entity);
            AddComponent(entity, new ZombieProperty
            {
                entity = entity,
                speedAvoid = authoring.speedAvoid,
                comparePriorities = authoring.comparePriorities,
                minDistanceAvoid = authoring.minDistanceAvoid,
            });
            AddComponent(entity, new ZombieSpawner
            {
                spawnInfinity = authoring.spawnInfinity,
                allowRespawn = authoring.allowRespawn,
                cooldown = authoring.cooldown,
                totalNumber = authoring.totalNumber,
                spawnAmountRange = authoring.spawnAmountRange,
                timeRange = authoring.timeRange,
                allowSpawnZombie = authoring.allowSpawnZombie,
                allowSpawnBoss = authoring.allowSpawnBoss,
            });

            // Add buffer zombie normal id spawn
            var bufferZombieNormalId = AddBuffer<BufferZombieNormalSpawnID>(entity);
            foreach (int id in authoring.zombieNormalIds)
            {
                bufferZombieNormalId.Add(new BufferZombieNormalSpawnID()
                {
                    id = id,
                });
            }
            //

            // Add buffer zombie Spawn
            var bufferZombieSpawn = AddBuffer<BufferZombieSpawnRange>(entity);
            foreach (var spawnRange in authoring.spawnRanges)
            {
                float3 posMin = spawnRange.pointRange1.position;
                float3 posMax = spawnRange.pointRange2.position;
                float3 dirNormal = math.normalize(spawnRange.pointDir2.position - spawnRange.pointDir1.position);
                bufferZombieSpawn.Add(new()
                {
                    posMax = posMax,
                    posMin = posMin,
                    directNormal = dirNormal,
                    destination = spawnRange.pointDir2.position,
                });
            }
            //

            // Add buffer zombie boss spawn
            var bufferZombieBossSpawn = AddBuffer<BufferZombieBossSpawn>(entity);
            foreach (var boss in authoring.spawnBosses)
            {
                float3 dirNormal = math.normalize(boss.pointDir2.position - boss.pointDir1.position);
                bufferZombieBossSpawn.Add(new BufferZombieBossSpawn()
                {
                    id = boss.id,
                    timeDelay = boss.timeDelay,
                    directNormal = dirNormal,
                    position = boss.spawnPos.position,
                    destination = boss.pointDir2.position,
                });
            }
            //
        }
    }
}


[Serializable]
public struct SpawnRange
{
    [Tooltip(
        "pointRange1 va pointRange2 là thông tin vùng spawn, vị trí spawn sẽ được random trong khoảng pointRange1 và pointRange2")]
    public Transform pointRange1;

    public Transform pointRange2;

    [Tooltip(
        "PointDir1 và PointDir2 sẽ thông tin hướng di chuyển, boss sẽ di chuyển theo hường từ PointDir1 và PointDir2")]
    public Transform pointDir1;

    public Transform pointDir2;
}

[Serializable]
public struct SpawnBoss
{
    [Tooltip("ID của boss")] public int id;

    [Tooltip("Thời gian delay spawn sau khi play game")]
    public float timeDelay;

    [Tooltip("Vị trí spawn ra boss")] public Transform spawnPos;

    [Tooltip(
        "PointDir1 và PointDir2 sẽ thông tin hướng di chuyển, boss sẽ di chuyển theo hường từ PointDir1 và PointDir2")]
    public Transform pointDir1;

    public Transform pointDir2;
}