using System;
using Unity.Entities;
using UnityEngine;

public class ECSCamera : MonoBehaviour
{
    [Header("References")]
    public Camera    mainCamera;
    public Transform playerCamera;
    public Transform characterCamera;
    public PlayerInfoECS playerInfoECS;
    public InputToECS inputToECS;
    
    
    [Header("Data")]
    public float     speedChangeCamera;
    public CameraType defaultType;
    public CameraInfo[] cameraInfos;
    
    private Transform     _mainCameraTf;
    private float         _progressChangeCamera;
    private int           _currentCameraIndex;
    private EntityManager _entityManager;
    private Entity        _entity;

    private CameraInfo _currentCameraInfo;
    private CameraInfo _nextCameraInfo;
    
    private void Awake()
    {
        _mainCameraTf = mainCamera.GetComponent<Transform>();

        if (playerInfoECS)
        {
            playerInfoECS.OnPlayerInfoChange    += UpdatePlayerCamera;
            playerInfoECS.OnCharacterInfoChange += UpdateCharacterCamera;
        }   
    }

    private void OnDestroy()
    {
        if (playerInfoECS)
        {
            playerInfoECS.OnPlayerInfoChange -= UpdatePlayerCamera;
        }
    }

    private void Start()
    {
        var typeCamStart = LoadTypeCamStart();
        _currentCameraInfo.rotationCamera = Quaternion.identity;
        // CreateEntityCameraECS(typeCamStart);
        ChangeCamInfo(typeCamStart);
        _progressChangeCamera = 1;
        UpdatePositionCam();
    }

    private void CreateEntityCameraECS(CameraType type)
    {
        _entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
        
        EntityArchetype archetype = _entityManager.CreateArchetype(
                typeof(CameraMode)
        );
        // Tạo thực thể
        _entity = _entityManager.CreateEntity(archetype);

        UpdateModeCam(type);
    }

    private void UpdateModeCam(CameraType type)
    {
        return;
        var camMode = new CameraMode()
        {
                cameraType = type,
        };
        _entityManager.SetComponentData(_entity, camMode);
    }

    private CameraType LoadTypeCamStart()
    {
        return defaultType;
    }

    private void ChangeCamInfo(int index)
    {
        if(cameraInfos.Length == 0) return;
        if (index < 0 || index >= cameraInfos.Length)
        {
            index = 0;
        }
        _nextCameraInfo        = cameraInfos[index];
        mainCamera.cullingMask = _nextCameraInfo.layerCullingForCam;
        _progressChangeCamera  = 0;
        _currentCameraIndex    = index;
        UpdateModeCam(_nextCameraInfo.typeCam);

    }
    
    private void ChangeCamInfo(CameraType type)
    {
        for(var i = 0; i < cameraInfos.Length; i++)
        {
            if (!cameraInfos[i].typeCam.Equals(type))
            {
                continue;
            }

            ChangeCamInfo(i);
            break;
        }
    }
    

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.V))
        {
            ChangeCamInfo(_currentCameraIndex + 1);
        }

        if (_progressChangeCamera < 1)
        {
            UpdatePositionCam();
        }

        if (_nextCameraInfo.typeCam != CameraType.ThirstPersonCamera)
        {
            UpdateCharacterCamera(inputToECS.RotationOnHelicopterMode);
        }
    }

    private void UpdatePlayerCamera(Vector3 position, Quaternion rotation)
    {
        playerCamera.position = position;
        playerCamera.rotation = rotation;
    }

    private void UpdateCharacterCamera(Quaternion rotation)
    {
        if(_nextCameraInfo.typeCam == CameraType.ThirstPersonCamera)
            return;
        characterCamera.rotation = rotation;
    }
    

    private void UpdatePositionCam()
    {
        _progressChangeCamera = Mathf.Clamp(_progressChangeCamera + speedChangeCamera * Time.deltaTime,0,1);
        var nextPosition = Vector3.Lerp(_currentCameraInfo.positionCamera, _nextCameraInfo.positionCamera,
                _progressChangeCamera);
       var  nextRotation = Quaternion.Lerp(_currentCameraInfo.rotationCamera,_nextCameraInfo.rotationCamera,_progressChangeCamera);
        _mainCameraTf.localPosition = nextPosition;
        _mainCameraTf.localRotation = nextRotation;

        if(_nextCameraInfo.typeCam == CameraType.ThirstPersonCamera)
        {
            characterCamera.rotation = Quaternion.Lerp(characterCamera.rotation,Quaternion.Euler(Vector3.zero), _progressChangeCamera);
        }
        
        if (_progressChangeCamera - 1 == 0)
        {
            _currentCameraInfo = _nextCameraInfo;
        }
    }

    #region Func ContextMenu

    [ContextMenu("LoadDefaultCamera")]
    private void LoadDataFirstCamera()
    {
        ChangeCamInfo(defaultType);
        _progressChangeCamera = 1;
        UpdatePositionCam();
    }
    
    #endregion
}
[Serializable]
public struct CameraInfo
{
    public CameraType typeCam;
    public Vector3    positionCamera;
    public Quaternion rotationCamera;
    public LayerMask  layerCullingForCam;
}