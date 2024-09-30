using UnityEngine;

[CreateAssetMenu( menuName = "DataSO/PlayerSO", order = 0)]
public class PlayerData : ScriptableObject
{
    [Tooltip("Prefab của nhân vật.")]
    public GameObject characterPrefab;

    [Tooltip("Điểm HP của người chơi.")]
    public float hp = 100;

    [Tooltip("Tốc độ di chuyển của người chơi.")]
    public float speed = 3.2f;

    [Tooltip("Bán kính của mỗi nhân vật để khác định va chạm")]
    public float radius = 3.5f;
}