using System;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

public class InputToECS : MonoBehaviour
{
    public Quaternion RotationOnHelicopterMode => _currentRotaQ;
    //
    public float _sensitive = 0.3f;
    //
    private EntityManager _entityManager;
    private Entity _entity;
    private PlayerInput _data;
    private Camera _camera;
    //
    private Vector3    _currentRota;
    private Quaternion _currentRotaQ;
    private CameraType _cameraType;
    private Config     _config;

    private void Awake()
    {
        Cursor.visible   = false;
        Cursor.lockState = CursorLockMode.Locked;
        EventDispatcher.Instance.RegisterListener(EventID.ChangeCamMode, ChangeCamMode);
    }

    private void ChangeCamMode(object obj)
    {
        _cameraType = (CameraType)obj;
    }

    void Start()
    {
        _config        = DataShare.Instance.config;
        _entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
        
        // Tạo một archetype cho thực thể của bạn
        EntityArchetype archetype = _entityManager.CreateArchetype(
            typeof(PlayerInput)
        );

        // Tạo thực thể
        _entity = _entityManager.CreateEntity(archetype);

        // Gán giá trị cho thành phần dữ liệu
        PlayerInput _data = new PlayerInput
        {
            directMove = float2.zero,
            pullTrigger = false
        };
        _entityManager.SetComponentData(_entity, _data);
        _camera = Camera.main;
    }

    void LateUpdate()
    {
        var mousePositionScreen = Input.mousePosition;
        mousePositionScreen.z = 20;
        var mousePositionWorld = Camera.main.ScreenToWorldPoint(mousePositionScreen);
        _data.directMove    = new(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
        _data.pullTrigger   = Input.GetMouseButton(0);
        _data.directMouse   = _camera.ScreenPointToRay(Input.mousePosition).direction;
        _data.mousePosition = mousePositionWorld;
        UpdateQuaternionRota();
        _data.rotationOnHelicopterMode = _currentRotaQ;
        _data.rota3D                   = _config.rota3DMode || _config.helicopterMode;
        _entityManager.SetComponentData(_entity, _data);
    }

    private void UpdateQuaternionRota()
    {
        var mousePositionNew = new Vector2(Input.GetAxisRaw("Mouse X"),Input.GetAxisRaw("Mouse Y")) * (_sensitive * Time.deltaTime);
        _currentRota.x -= mousePositionNew.y;
        _currentRota.y += mousePositionNew.x;
        
        if (!(_config.rota3DMode || _config.helicopterMode) || _cameraType == CameraType.ThirstPersonCamera)
        {
            _currentRota.x = 0;
        }
        else
        {
            _currentRota.x =  Mathf.Clamp(_currentRota.x, -90, 90);
        }
        
        _currentRotaQ  =  Quaternion.Euler(_currentRota.x, _currentRota.y, 0);
    }
}