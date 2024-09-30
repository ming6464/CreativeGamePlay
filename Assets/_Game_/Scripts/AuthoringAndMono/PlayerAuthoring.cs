using System;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

public class PlayerAuthoring : MonoBehaviour
{
    [Tooltip("Vị trí spawn của người chơi.")]
    public Transform spawnPosition;

    [Tooltip("Khoảng cách để nhắm mục tiêu khi bật chế độ tự động nhắm.")]
    private float _distanceAim = 300;
    
    [Header("Config")]
    public Config config;
    
    [Header("Helicopter MODE Setup")]
    public NextDestinationInfo[] nextDestinationInfos;

    class AuthoringBaker : Baker<PlayerAuthoring>
    {
        public override void Bake(PlayerAuthoring authoring)
        {
            Entity entity = GetEntity(TransformUsageFlags.None);

            AddComponent(entity, new PlayerProperty
            {
                characterEntity      = GetEntity(authoring.config.playerData.characterPrefab, TransformUsageFlags.Dynamic),
                speed                =  authoring.config.playerData.speed,
                spawnPosition        = authoring.spawnPosition.position,
                spaceGrid            = authoring.config.spaceGrid,
                numberSpawnDefault   = authoring.config.numberSpawnDefault,
                aimType              = authoring.config.aimType,
                characterRadius      = authoring.config.playerData.radius,
                idWeaponDefault      = authoring.config.idWeaponDefault,
                distanceAim          = authoring._distanceAim,
                moveToWardMin        = authoring.config.moveToWardMin,
                moveToWardMax        = authoring.config.moveToWardMax,
                speedMoveToNextPoint = authoring.config.speedMoveToNextPoint,
                hp                   =  authoring.config.playerData.hp,
                aimNearestEnemy      = authoring.config.aimNearestEnemy,
                helicopterMode       = authoring.config.helicopterMode,
            });

            if (authoring.config.helicopterMode)
            {
                var bufferNextDestination = AddBuffer<bufferMoveDestination>(entity);

                foreach (var nextDestination in authoring.nextDestinationInfos)
                {
                    bufferNextDestination.Add(new bufferMoveDestination()
                    {
                        position = nextDestination.point.position,
                        speed = nextDestination.speed,
                    });
                }
            }
        }
    }

    private void OnDrawGizmos()
    {
        if(!config.helicopterMode) return;
        Vector3 passPosition = default;
        for(int i = 0; i < nextDestinationInfos.Length; i++)
        {
            var nextDestination = nextDestinationInfos[i];
            if (i != 0)
            {
                Gizmos.DrawLine(passPosition,nextDestination.point.position);
            }
            passPosition = nextDestination.point.position;
            Gizmos.DrawSphere(passPosition,0.2f);
        }
    }

    [Serializable]
    public struct NextDestinationInfo
    {
        public Transform point;
        public float speed;
    }
}

