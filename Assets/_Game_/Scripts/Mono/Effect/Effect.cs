using System;
using UnityEngine;

public class Effect : MonoBehaviour
{
    public ParticleSystem[] effects;
    public EffectID effectID;
    public bool pushToPoolWhenFinish;
    
    protected Transform _tf;
    protected bool _playing;

    private void Awake()
    {
        _tf = transform;
    }

    public virtual void Play(Vector3 position, Quaternion rotation)
    {
        _playing = true;
        _tf.position = position;
        _tf.rotation = rotation;
        foreach (var e in effects)
        {
            e.Emit(1);
        }
    }
}