using Cinemachine;
using UnityEngine;
using UnityEngine.Rendering.Universal;

public class CameraManager : MonoBehaviour
{
    static public CameraManager instance;
    private void Awake()
    {
        instance = this;
    }

    [SerializeField] private CinemachineVirtualCamera[] Cameras = new CinemachineVirtualCamera[2];
    private CinemachineConfiner2D[] Confiners = new CinemachineConfiner2D[2];
    [SerializeField] private CinemachineBrain CBrain;
    private Camera theCamera;

    [SerializeField] private BGRenderer bgrenderer;

    private CinemachineImpulseSource impulseSource;
    public float OriginalRes = 5;
    private int CurrentCamera = 0; // 0 or 1
    public bool freeze = false;
    public Transform currentFollowTarget;

    private void Start()
    {
        theCamera = GetComponent<Camera>();
        this.transform.position = currentFollowTarget.position;
        for (int i = 0; i < Cameras.Length; i++)
        {
            Confiners[i] = Cameras[i].GetComponent<CinemachineConfiner2D>();
        }
        Cameras[CurrentCamera].Priority = 11;
        ReturntoOrigRes(0);

        impulseSource = GetComponent<CinemachineImpulseSource>();
        UpdateConfiner(this.transform.position);
    }

    public void TogglePostProcessing(bool _enable) =>
    theCamera.GetUniversalAdditionalCameraData().renderPostProcessing = _enable;

    public void StrictMovement(Vector3 _targetPos, Vector3 _prevPos)
    {
        Vector3 way = _targetPos - _prevPos;
        for (int i = 0; i < Cameras.Length; i++)
        {
            Cameras[i].OnTargetObjectWarped(Cameras[i].Follow, way);
        }
        this.transform.position = _targetPos;

        UpdateConfiner(_targetPos);
    }

    private void UpdateConfiner(Vector3 _pos)
    {
        Collider2D other = Physics2D.OverlapCircle(_pos, 0.5f, LayerMask.GetMask("CameraBound"));
        if (other)
        {
            for (int i = 0; i < Cameras.Length; i++)
            {
                Confiners[i].m_BoundingShape2D = other;
                Confiners[i].m_MaxWindowSize = 0;
            }
        }
        else
        {
            for (int i = 0; i < Cameras.Length; i++)
            {
                Confiners[i].m_BoundingShape2D = null;
                Confiners[i].m_MaxWindowSize = 0;
            }
        }
    }

    // 0 to 1, 1 to 0
    private int FlipNum(int _i) => (_i + 1) % 2;

    // OWO
    public void Zoom(float _res, float _blendTime,
    CinemachineBlendDefinition.Style _style = CinemachineBlendDefinition.Style.EaseOut)
    {
        CurrentCamera = FlipNum(CurrentCamera);
        CBrain.m_DefaultBlend.m_Time = _blendTime;
        CBrain.m_DefaultBlend.m_Style = _style;
        Cameras[CurrentCamera].m_Lens.OrthographicSize = _res;

        Cameras[CurrentCamera].Priority = 11;
        Cameras[FlipNum(CurrentCamera)].Priority = 10;
        Confiners[CurrentCamera].m_MaxWindowSize = _res + 0.1f;
    }

    public void ZoomViaOrig(float _multiplyer, float _blendTime,
    CinemachineBlendDefinition.Style _style = CinemachineBlendDefinition.Style.EaseOut)
    {
        Zoom(OriginalRes * _multiplyer, _blendTime, _style);
    }

    // Set original resolution
    public void SetOrigRes(float _res) => OriginalRes = _res;


    // Set current resolution to original resolution
    public void ReturntoOrigRes(float _blendTime,
    CinemachineBlendDefinition.Style _style = CinemachineBlendDefinition.Style.EaseOut)
    {
        Zoom(OriginalRes, _blendTime, _style);
    }

    public void SetBound(AreaInfo _areaInfo)
    {
        for (int i = 0; i < Cameras.Length; i++)
        {
            Confiners[i].m_BoundingShape2D = _areaInfo.cameraBound;
        }
    }

    public void SetBG(AreaInfo _areaInfo)
    {
        bgrenderer.RenderBackGround(_areaInfo.backGround);
    }

    public void Shake()
    {
        impulseSource.GenerateImpulse();
    }

    public Vector3 GetCurrentCameraPos()
    {
        return Cameras[CurrentCamera].transform.position;
    }
}
