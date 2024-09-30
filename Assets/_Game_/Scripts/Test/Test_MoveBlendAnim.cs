using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

public class Test_MoveBlendAnim : MonoBehaviour
{

    public float speed;
    public float animX;
    public float animY;
    public float x;
    public float y;
    private Animator _animator;
    private Inputs _inputs;
    private static readonly int _x = Animator.StringToHash("X");
    private static readonly int _y = Animator.StringToHash("Y");
    private static readonly int _animX = Animator.StringToHash("AimX");
    private static readonly int _animY = Animator.StringToHash("AimY");


    private void Awake()
    {
        _inputs = new Inputs();
        _animator = GetComponent<Animator>();
    }

    // Start is called before the first frame update
    void Start()
    {
        Cursor.lockState = Cursor.lockState;
        Cursor.visible = false;
        _inputs.Enable();
        
    }

    // Update is called once per frame
    void Update()
    {
        
        Vector2 curMoveInput = _inputs.Player.PlayerMovement.ReadValue<Vector2>();
        Vector2 mousePos = _inputs.Player.Mouse.ReadValue<Vector2>();

        if (mousePos != Vector2.zero)
        {
            mousePos = math.normalize(mousePos);
            mousePos.y = Mathf.Clamp(mousePos.y, 0, 1);
        }
        
        x = Mathf.MoveTowards(x,curMoveInput.x,Time.deltaTime * speed);
        y = Mathf.MoveTowards(y,curMoveInput.y,Time.deltaTime * speed);
        
        animX = mousePos.x;
        animY = mousePos.y;

        _animator.SetFloat(_x,x);
        _animator.SetFloat(_y,y);
        _animator.SetFloat(_animX,animX);
        _animator.SetFloat(_animY,animY);

    }
}
