using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

public class ActiveZoneAuthoring : MonoBehaviour
{
    [Tooltip("Vùng hoạt động của zombie nếu zombie ở ngoài cùng pointRangeMin tới pointRangeMax thì sẽ tự động die")]
    public Transform pointRangeMin;
    public Transform pointRangeMax;
}

class ActiveZoneAuthoringBaker : Baker<ActiveZoneAuthoring>
{
    public override void Bake(ActiveZoneAuthoring authoring)
    {
        Entity entity = GetEntity(TransformUsageFlags.None);
        AddComponent(entity,new ActiveZoneProperty()
        {
            pointRangeMin = authoring.pointRangeMin.position,
            pointRangeMax = authoring.pointRangeMax.position,
        });
    }
}

