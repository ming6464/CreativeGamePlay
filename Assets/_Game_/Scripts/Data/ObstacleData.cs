using System;
using Unity.Mathematics;
using UnityEngine;

namespace _Game_.Scripts.Data
{
    [CreateAssetMenu(menuName = "DataSO/ObstacleSO")]
    public class ObstacleSO : ScriptableObject
    {
        public ObstacleItem[] obstacles;
    }

    [Serializable]
    public struct ObstacleItem
    {
        public int id;
        public Obstacle obstacle;
    }

    public abstract class Obstacle : ScriptableObject
    {
        public GameObject prefabs;
        public ItemType type;
    }
    
}