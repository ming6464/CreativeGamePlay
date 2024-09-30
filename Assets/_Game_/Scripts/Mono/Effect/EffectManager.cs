using System;
using Unity.Entities;
using UnityEngine;

public class EffectManager : MonoBehaviour
{
    public effectInfo[] effectInfos;
    private bool _isAddEvent;
    
    private void Update()
    {
        if (!_isAddEvent)
        {
            UpdateHybrid playerSystem = World.DefaultGameObjectInjectionWorld.GetExistingSystemManaged<UpdateHybrid>();
            if(playerSystem == null) return;
            _isAddEvent = true;
            playerSystem.UpdateHitFlashEff += UpdateHitFlashEff;
        }
    }

    private void UpdateHitFlashEff(Vector3 position,Quaternion rotation,EffectID effectID)
    {
        foreach (var i in effectInfos)
        {
            if (i.effect.effectID == effectID)
            {
                if (i.effect.pushToPoolWhenFinish)
                {
                    var eff = ObjectPool.Instance.PopFromPool(effectID, i.gameObject);
                    eff.SetActive(true);
                    eff.GetComponent<Effect>().Play(position,rotation);
                }
                else
                {
                    i.effect.Play(position,rotation);
                }
                
            }
        }
    }
    
    [Serializable]
    public struct effectInfo
    {
        public Effect effect;
        public GameObject gameObject;
    }
    
}