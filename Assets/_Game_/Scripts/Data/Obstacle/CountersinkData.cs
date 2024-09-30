using _Game_.Scripts.Data;
using UnityEngine;

[CreateAssetMenu(menuName = "DataSO/CouterSinkSO")]
public class CountersinkSO : Obstacle
{
    public float lifeTime;
    public float radius;
    public float height;
    public float damage;
    public float delayTime;
    public float cooldown;
}