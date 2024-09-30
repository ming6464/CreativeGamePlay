using System.Threading.Tasks;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

public class ItemManager : MonoBehaviour
{
    [SerializeField]
    private Item counterSinkItemPrefab;

    private void Start()
    {
        AddEvent();
    }

    //
    private async void AddEvent()
    {
        while (true)
        {
            await Task.Yield();
            UpdateHybrid updateHybrid = World.DefaultGameObjectInjectionWorld.GetExistingSystemManaged<UpdateHybrid>();

            if (updateHybrid == null)
            {
                continue;
            }

            updateHybrid.ApplyEventItem += ApplyEventItem;

            break;
        }
    }

    private void ApplyEventItem(NativeArray<BufferSpawnPoint> bufferspawnpoints, ItemPropertyEvent itempropertyevent,
                                EventTypeOnMono               type)
    {
        switch (type)
        {
            case EventTypeOnMono.CountersinkItem:
                var items = SpawnItem(counterSinkItemPrefab, bufferspawnpoints);
                foreach (var item in items)
                {
                    item.timeLife  = itempropertyevent.lifeTime;
                    item.damage    = itempropertyevent.damage; 
                    item.delayTime = itempropertyevent.delayTime;
                    item.radius    = itempropertyevent.radius;
                    item.height    = itempropertyevent.height;
                    item.ResetState();
                    item.Begin();
                }

                break;
        }
    }

    private Item[] SpawnItem(Item item, NativeArray<BufferSpawnPoint> bufferSpawnPoints)
    {
        var itemSpawn = new Item[bufferSpawnPoints.Length];

        for (int i = 0; i < bufferSpawnPoints.Length; i++)
        {
            itemSpawn[i] = Instantiate(item, bufferSpawnPoints[i].value, Quaternion.identity);
        }

        return itemSpawn;
    }

}