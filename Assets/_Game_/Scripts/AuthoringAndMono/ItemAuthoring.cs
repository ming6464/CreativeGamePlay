using System;
using System.Collections.Generic;
using System.Globalization;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

public class ItemAuthoring : MonoBehaviour
{
    [Tooltip("Xác định loại item của đối tượng.")]
    public ItemType type;

    [Tooltip("Xác định cách sử dụng của item.")]
    public TypeUsing typeUsing;

    [Tooltip("ID duy nhất của item.")]
    public int id;

    [Header("Setup")]
    [Space(10)]
    [Tooltip("Nếu item là loại bắn đạn để sử dụng thì số này sẽ là số hp của nó\nNếu item là loại khi sử dụng sẽ tăng số lượng nhân vật thì số này sẽ là số để tính toán")]
    public int count;

    [Tooltip("Xác định loại phép tính đến item.")]
    public Operation operation;

    [Tooltip("Vị trí của text liên quan đến item.")]
    public Transform textPositon;
    
    [Header("Item CanShooting setup bên dưới")]
    [Space(10)]
    [Tooltip("Xác định liệu item có xoay theo hướng người chơi hay không.")]
    public bool followPlayer;

    [Header("Item Spawn")]
    [Tooltip("Mảng các điểm spawn cho item.\nví dụ: item pháo, khi đây là mảng các vị trí sẽ spawn ra chúng")]
    public List<BufferSpawnPoint> spawnPoints;

    [Tooltip("Bật/Tắt vẽ")] 
    public bool isDraw;

    [Tooltip("Cập nhật liên tục vị trí spawn point")] 
    public bool constantlyUpdated;
    
    [Tooltip("Chênh lệch vị trí so với item")]
    public Vector3 offset = Vector3.zero;

    [Tooltip("Kích thước lưới spawn")] 
    public Vector2 gridSize = Vector2.right;
    
    [Tooltip("Khoảng cách mỗi điểm spawn trên lưới")]
    public Vector2 gridSpace = Vector2.one;

    [Tooltip("Bán kính mỗi điểm spawn > chỉ có tác dụng minh hoạ vẽ")]
    public float radiusPoint = 1f;
    
    [Header("-----Phần này bỏ qua-----")]
    [Tooltip("Mảng các thiết lập thông tin item cho vũ khí.")]
    public ItemInfoSetup[] weapons;
    public Animator animator;
    
    //
    private int _passIdWeapon;
    private ItemType _passItemType;

    private void OnValidate()
    {
        if(Application.isPlaying) return;
        if (typeUsing == TypeUsing.canShooting && _passIdWeapon != id || _passItemType != type)
        {
            ChangeModel();
        }
        CalculateSpawnPoint();
    }

    private void ChangeModel()
    {
        if (id >= 0)
        {
            foreach (var weapon in weapons)
            {
                if (weapon.id == id && weapon.itemType == type)
                {
                    weapon.weapon.SetActive(true);
                    animator.avatar = weapon.avatar;
                        
                }
                else
                {
                    weapon.weapon.SetActive(false);
                }
                    
            }
            _passIdWeapon = id;
            _passItemType = type;
        }
    }

    private void CalculateSpawnPoint()
    {
        spawnPoints = new List<BufferSpawnPoint>();
        var totalPoint = gridSize.x * gridSize.y;

        var offsetGrid = new Vector3(-gridSize.x /2f +0.5f,0,0);
            
        for (int i = 0; i < totalPoint; i++)
        {
            var posInGrid = PointPositionInGrid(i);
            var posApplyOffset = posInGrid + offsetGrid;
            posApplyOffset.x *= gridSpace.x;
            posApplyOffset.z *= gridSpace.y;
            spawnPoints.Add(new BufferSpawnPoint()
            {
                value = transform.TransformPoint(posApplyOffset + offset),
                posInGrid = (float3)posInGrid,
            });
        }
    }

    private Vector3 PointPositionInGrid(int index)
    {
        int y = (int)(index / gridSize.x);
        int x = index - (int)(y * gridSize.x);

        return new Vector3(x,0, y);
    }

    private void OnDrawGizmos()
    {
        if(Application.isPlaying) return;
        if (constantlyUpdated)
        {
            CalculateSpawnPoint();
        }
        if(!isDraw && spawnPoints != null) return;
        Gizmos.color = new Color(1, 0, 0, 0.5f);
        foreach (var point in spawnPoints)
        {
            Gizmos.DrawCube((Vector3)point.value + Vector3.up * (radiusPoint / 2f),Vector3.one * radiusPoint);
        }
    }

    private class ItemAuthoringBaker : Baker<ItemAuthoring>
    {
        public override void Bake(ItemAuthoring authoring)
        {
            var textObj = authoring.textPositon;
            Entity entity = GetEntity(TransformUsageFlags.None);
            AddComponent(entity,new ItemInfo()
            {
                id = authoring.id,
                type = authoring.type,
                count = authoring.count,
                hp = authoring.count,
                operation = authoring.operation,
                idTextHp = entity.Index,
            });
            
            AddComponent<HitCheckOverride>(entity);
            
            if (authoring.typeUsing.Equals(TypeUsing.canShooting))
            {
                AddComponent<ItemCanShoot>(entity);

                var text = new TextPropertyEvent()
                {
                    id = entity.Index,
                    number = authoring.count,
                    position = textObj.parent.position,
                    offset =  textObj.position - textObj.parent.position,
                    textFollowPlayer = authoring.followPlayer,
                };
                
                AddComponent(entity,new EventPlayOnMono()
                {
                    eventTypeOnMono = EventTypeOnMono.ChangeText,
                    textPropertyEvent = text,
                });
            }else if (authoring.type.Equals(ItemType.Character))
            {
                string str = "";
                switch (authoring.operation)
                {
                    case Operation.Addition:
                        str = "+";
                        break;
                    case Operation.Subtraction:
                        str = "-";
                        break;
                    case Operation.Multiplication:
                        str = "x";
                        break;
                    case Operation.Division:
                        str = ":";
                        break;
                }

                var text = new TextPropertyEvent()
                {
                    id = entity.Index,
                    text = str,
                    number = authoring.count,
                    position = textObj.parent.position,
                    offset =  textObj.position - textObj.parent.position,
                    textFollowPlayer = authoring.followPlayer
                };
                
                AddComponent(entity,new EventPlayOnMono()
                {
                    eventTypeOnMono = EventTypeOnMono.ChangeText,
                    textPropertyEvent = text,
                });
            }
            
            if (authoring.spawnPoints.Count > 0)
            {
                var buffer = AddBuffer<BufferSpawnPoint>(entity);

                foreach (var point in authoring.spawnPoints)
                {
                    buffer.Add(point);
                }
            }
        }
    }
}

public enum TypeUsing
{
    none,
    canShooting
}

public enum Operation
{
    Addition, Subtraction, Multiplication, Division
}

[Serializable]
public struct ItemInfoSetup
{
    public GameObject weapon;
    public Avatar avatar;
    public int id;
    public ItemType itemType;
}