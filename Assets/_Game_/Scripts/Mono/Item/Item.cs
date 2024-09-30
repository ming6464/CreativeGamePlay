using System;
using UnityEngine;

public abstract class Item : MonoBehaviour
{
    [HideInInspector]
    public float timeLife;
    [HideInInspector]
    public float damage;
    [HideInInspector]
    public float delayTime;
    [HideInInspector]
    public float radius;
    [HideInInspector]
    public float height;

    protected bool isPlaying;
    protected float delayTimeDelta;
    protected float startTime;
    
    public void ResetState()
    {
        isPlaying = false;
    }

    public void Begin()
    {
        delayTimeDelta = delayTime;
    }

    private void Update()
    {
        if (delayTimeDelta >= 0)
        {
            delayTimeDelta -= Time.deltaTime;
            if (delayTimeDelta <= 0)
            {
                delayTimeDelta = -1;
                isPlaying = true;
                startTime = Time.time;
                OnPlay();
            }
        }
        if(!isPlaying) return;
        OnUpdateVirtual();
        CheckDead();
    }

    private void CheckDead()
    {
        if(Time.time - startTime < timeLife) return;
        isPlaying = false;
        OnDead();
    }

    public abstract void OnPlay();

    public virtual void OnUpdateVirtual()
    {
        
    }
    public abstract void OnDead();


}