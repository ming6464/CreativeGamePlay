using Unity.Mathematics;
using UnityEngine;

namespace _Game_.Scripts.Data
{
    [CreateAssetMenu(menuName = "DataSO/TurretSO")]
    public class TurrentSO : Obstacle
    {
        public float timeLife;
        public float3 pivotFireOffset;
        public int bulletPerShot;
        public float spacePerBullet;
        public float spaceAnglePerBullet;
        public bool parallelOrbit;
        public float speed;
        public float damage;
        public float cooldown;
        public float distanceAim;
        public float moveToWardMax;
        public float moveToWardMin;
    }
}