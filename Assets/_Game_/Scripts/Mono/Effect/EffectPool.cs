using UnityEngine;

public class EffectPool : Effect
{
    public float lifeTime;
    private float _timer;
    private void Update()
    {
        if(!_playing) return;
        if (_timer > lifeTime)
        {
            ObjectPool.Instance.PushToPool(effectID,gameObject);
            return;
        }

        _timer += Time.deltaTime;
    }

    public void Init()
    {
        _tf = transform;
        foreach (var particle in effects)
        {
            particle.Stop();
        }
    }

    public void OnPushToPool()
    {
        _playing = false;
    }
    public override void Play(Vector3 position, Quaternion rotation)
    {
        _playing = true;
        _tf.position = position;
        _tf.rotation = rotation;
        _timer = 0;
        foreach (var particle in effects)
        {
            particle.Play();
        }
    }
}

