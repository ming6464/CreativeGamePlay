using System;
using UnityEngine;

public class FollowPlayer : MonoBehaviour
{
    //
    private Transform _playerTf;
    private bool _hasPlayer;
    private void Start()
    {
        if (PlayerInfoECS.Instance)
        {
            _playerTf = PlayerInfoECS.Instance.transform;
            _hasPlayer = true;
        }
    }

    private void Update()
    {
        FollowRotate();
    }

    private void FollowRotate()
    {
        if(!_hasPlayer ) return;
        var euler = Quaternion.LookRotation(transform.position - _playerTf.position).eulerAngles;
        euler.x = 0;
        euler.z = 0;
        transform.rotation = Quaternion.Euler(euler);
    }
}