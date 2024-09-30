using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

public class WeaponAuthoring : MonoBehaviour
{
    [Tooltip("Xác định liệu có bắn tự động hay không.")]
    public bool shootAuto;

    [Tooltip("Prefab của viên đạn.")]
    public GameObject bulletPrefab;

    [Tooltip("Thời gian tồn tại của viên đạn.")]
    public float timeLife;

    [Tooltip("Phạm vi sát thương.")]
    public float2 damageRangeRatio;

    [Tooltip("Phạm vi thời gian để điều chỉnh số lượng spawn mỗi lần\n\nví dụ:\ndamageRangeRatio = (10, 100)\ntimeRange = (1, 10)\n\nThời gian tính từ lúc chạy game là time\nsát thương đạn damage\n\nnếu time <= 1 thì damage = 10\nnếu time >= 10 thì damage = 100\ncòn lại damage sẽ nằm trong khoảng 10 -> 100 dựa theo time")]
    public float2 timeRange;
    
    class WeaponBaker : Baker<WeaponAuthoring>
    {
        public override void Bake(WeaponAuthoring authoring)
        {
            Entity entity = GetEntity(TransformUsageFlags.None);
            AddComponent(entity,new WeaponProperty()
            {
                shootAuto = authoring.shootAuto,
                entityBullet = GetEntity(authoring.bulletPrefab,TransformUsageFlags.Dynamic),
                length = 0,
                timeLife = authoring.timeLife,
                damageRangeRatio = authoring.damageRangeRatio,
                timeRange = authoring.timeRange,
            });

            AddBuffer<BufferBulletDisable>(entity);
            AddBuffer<BufferBulletSpawner>(entity);
        }
    }

}


