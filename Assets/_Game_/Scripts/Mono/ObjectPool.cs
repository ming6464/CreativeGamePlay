using System.Collections.Generic;
using UnityEngine;

public class ObjectPool : Singleton<ObjectPool>
{
    private Dictionary<EffectID, Queue<GameObject>> _dictionaryPool;

    public override void Awake()
    {
        base.Awake();
        _dictionaryPool = new Dictionary<EffectID, Queue<GameObject>>();
    }

    public GameObject PopFromPool(EffectID effectID,GameObject prefab)
    {
        if (!_dictionaryPool.ContainsKey(effectID))
        {
            _dictionaryPool.Add(effectID,new Queue<GameObject>());
        }

        GameObject gobj;

        if (_dictionaryPool[effectID].Count > 0)
        {
            gobj = _dictionaryPool[effectID].Dequeue();
        }
        else
        {
            gobj = Instantiate(prefab);
        }
        
        return gobj;
    }

    public void PushToPool(EffectID effectID,GameObject gameObject)
    {
        if (!_dictionaryPool.ContainsKey(effectID))
        {
            _dictionaryPool.Add(effectID, new Queue<GameObject>());
        }
        gameObject.SetActive(false);
        _dictionaryPool[effectID].Enqueue(gameObject);
    }
}