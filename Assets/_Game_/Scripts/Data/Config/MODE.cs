using Unity.Mathematics;
using UnityEngine;

[CreateAssetMenu(fileName = "Config", menuName = "DataSO/MODE")]
public class Config : ScriptableObject
{
    public bool helicopterMode;
    public bool rota3DMode;

    [Space(10)]
    public PlayerData playerData;
    
    [Space(10)]
    [Tooltip("Số lượng nhân vật khi bắt đầu.")]
    public int numberSpawnDefault;
    
    [Tooltip("ID vũ khí của người chơi khi bắt đầu game.")]
    public int idWeaponDefault;
    
    [Tooltip("Khoảng cách giữa các nhân vật.")]
    public float2 spaceGrid;
    
    [Tooltip("Loại mục tiêu.")]
    public AimType aimType;
    
    [Tooltip("Tốc độ di chuyển của các nhân vật đến vị trí của hàng ngũ của mình")]
    public float speedMoveToNextPoint = 2.5f;

    [Tooltip("Xác định có tự động nhắm vào kẻ thù gần nhất hay không.")]
    public bool aimNearestEnemy = false;
    
    [Tooltip("Tốc độ xoay tối đa.")]
    public float moveToWardMax = 300;

    [Tooltip("Tốc độ xoay tối thiểu.")]
    public float moveToWardMin = 100;
}