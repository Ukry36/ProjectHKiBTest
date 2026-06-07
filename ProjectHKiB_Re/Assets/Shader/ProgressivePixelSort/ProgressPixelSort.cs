using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class ProgressPixelSort : MonoBehaviour
{
    [Header("Core Settings")]
    [Tooltip("수정된 HLSL이 연결된 셰이더 그래프 머티리얼")]
    public Material computeMaterial; 
    
    [Tooltip("글리치 텍스처 해상도 (스프라이트 크기와 비율에 맞게 조절하세요)")]
    public Vector2Int resolution = new Vector2Int(256, 256);

    [Range(1, 20)]
    [Tooltip("1프레임당 정렬 수행 횟수 (수치가 높을수록 픽셀이 빠르게 흘러내림)")]
    public int speedMultiplier = 2;

    private RenderTexture pingRT;
    private RenderTexture pongRT;
    private SpriteRenderer spriteRenderer;
    private MaterialPropertyBlock propBlock;
    public Camera mainCam;

    void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        propBlock = new MaterialPropertyBlock();
        //mainCam = Camera.main;

        // 1. 메모리에 빈 렌더 텍스처 2장(Ping/Pong) 생성
        pingRT = new RenderTexture(resolution.x, resolution.y, 0, RenderTextureFormat.ARGB32);
        pongRT = new RenderTexture(resolution.x, resolution.y, 0, RenderTextureFormat.ARGB32);
        pingRT.filterMode = FilterMode.Point;
        pongRT.filterMode = FilterMode.Point;

        // 투명하게 초기화 (알파 0을 인식해 셰이더가 배경 캡처를 시작함)
        Graphics.Blit(Texture2D.blackTexture, pingRT);
        Graphics.Blit(Texture2D.blackTexture, pongRT);
    }

    void LateUpdate()
    {
        if (computeMaterial == null) return;

        // 1. 스프라이트의 화면상 위치를 계산하여 셰이더로 전달
        Vector3 min = spriteRenderer.bounds.min;
        Vector3 max = spriteRenderer.bounds.max;
        Vector3 screenMin = mainCam.WorldToScreenPoint(min);
        Vector3 screenMax = mainCam.WorldToScreenPoint(max);

        Vector4 screenBounds = new Vector4(
            screenMin.x / mainCam.pixelWidth,
            screenMin.y / mainCam.pixelHeight,
            screenMax.x / mainCam.pixelWidth,
            screenMax.y / mainCam.pixelHeight
        );
        computeMaterial.SetVector("_ScreenBounds", screenBounds);
        computeMaterial.SetVector("_TexelSize", new Vector4(1f/resolution.x, 1f/resolution.y, resolution.x, resolution.y));

        // 2. SpeedMultiplier 만큼 프레임 내 반복 연산 (Ping-Pong Swap)
        RenderTexture targetRT = null;
        for (int i = 0; i < speedMultiplier; i++)
        {
            // 루프마다 짝홀을 번갈아가며 스왑 충돌 방지
            bool isEven = (Time.frameCount + i) % 2 == 0;
            
            computeMaterial.SetTexture("_PrevFrameTex", isEven ? pingRT : pongRT);
            computeMaterial.SetInt("_FrameParity", isEven ? 0 : 1);

            targetRT = isEven ? pongRT : pingRT;
            
            // source 없이 targetRT에 computeMaterial 결과물을 바로 그림
            Graphics.Blit(null, targetRT, computeMaterial); 
        }

        // 3. 연산이 완료된 최종 텍스처를 스프라이트 렌더러에 밀어넣기
        spriteRenderer.GetPropertyBlock(propBlock);
        propBlock.SetTexture("_MainTex", targetRT);
        spriteRenderer.SetPropertyBlock(propBlock);
    }

    void OnDestroy()
    {
        if (pingRT != null) pingRT.Release();
        if (pongRT != null) pongRT.Release();
    }
}