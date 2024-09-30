using System;
using UnityEngine;

public class CountersinkItem : Item
{
    [SerializeField] private Transform counterStickParent;
    [SerializeField] private float timeAnimStartEnd;
    private int _state;
    private float _timeAnimStartEndDelta;
    private Vector3 _startPosition;
    private Vector3 _endPosition;
    private Transform _myTf;

    public override void OnPlay()
    {
        _startPosition = Vector3.zero;
        _endPosition = new Vector3(0, height, 0);
        counterStickParent.localPosition = Vector3.zero;
        _timeAnimStartEndDelta = 0;
        _state = 0;
        _myTf = transform;
    }

    public override void OnDead()
    {
        DestroyImmediate(gameObject);
    }

    public override void OnUpdateVirtual()
    {
        base.OnUpdateVirtual();
        HandleState();
    }

    private void HandleState()
    {
        switch (_state)
        {
            case 0:
                _timeAnimStartEndDelta += Time.deltaTime;
                if (_timeAnimStartEndDelta >= timeAnimStartEnd)
                {
                    _timeAnimStartEndDelta = timeAnimStartEnd;
                    _state++;
                }
                counterStickParent.localPosition = Vector3.Lerp(_startPosition, _endPosition, _timeAnimStartEndDelta / timeAnimStartEnd);
                break;
            case 1:
                if (Time.time - startTime >= timeLife - timeAnimStartEnd)
                {
                    _state++;
                    _timeAnimStartEndDelta = 0;
                }
                break;
            case 2:
                _timeAnimStartEndDelta += Time.deltaTime;
                if (_timeAnimStartEndDelta >= timeAnimStartEnd)
                {
                    _timeAnimStartEndDelta = timeAnimStartEnd;
                    _state++;
                }
                counterStickParent.localPosition = Vector3.Lerp(_endPosition, _startPosition, _timeAnimStartEndDelta / timeAnimStartEnd);
                break;
        }
    }
}