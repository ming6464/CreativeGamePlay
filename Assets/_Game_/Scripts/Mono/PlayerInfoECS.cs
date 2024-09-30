using System;
using System.Threading.Tasks;
using Unity.Entities;
using UnityEngine;

public class PlayerInfoECS : Singleton<MonoBehaviour>
{
    public Action<Vector3, Quaternion> OnPlayerInfoChange;
    public Action<Quaternion> OnCharacterInfoChange;
    
    private bool _addEvent;

    private void Start()
    {
        AddEvent();
    }
    

    private async void AddEvent()
    {
        while (!_addEvent)
        {
            await Task.Yield();
            UpdateHybrid updateHybrid = World.DefaultGameObjectInjectionWorld.GetExistingSystemManaged<UpdateHybrid>();

            if (updateHybrid == null)
            {
                continue;
            }
            updateHybrid.UpdatePlayerInfo    += UpdatePlayerCamera;

            if (DataShare.Instance.config.aimNearestEnemy)
            {
                updateHybrid.UpdateCharacterInfo += UpdateCharacterCamera;
            }
            _addEvent                          =  true;
        }
    }

    private void UpdateCharacterCamera(Quaternion rotation)
    {
        OnCharacterInfoChange?.Invoke(rotation);
    }

    private void UpdatePlayerCamera(Vector3 position, Quaternion rotation)
    {
        OnPlayerInfoChange?.Invoke(position, rotation);
    }
}
