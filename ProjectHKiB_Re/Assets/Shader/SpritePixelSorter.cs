using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class SpritePixelSorter : MonoBehaviour
{
    public ComputeShader pixelSortShader;
    [Range(0f, 1f)] public float threshold = 0.3f;
    public bool verticalSort = false;
    
    [Tooltip("실시간 변형 스프라이트는 프레임당 루프 횟수가 높아야 끊기지 않고 완전한 정렬 상태를 유지합니다.")]
    [Range(1, 100)] public int iterationsPerFrame = 40;

    private SpriteRenderer spriteRenderer;
    private RenderTexture renderTexture;
    private Material customMaterial;
    private int kernelIndex;
    private Texture2D lastActiveTexture;

    void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        kernelIndex = pixelSortShader.FindKernel("CSPixelSort");

        // 오브젝트 고유의 인스턴스 머티리얼 참조 확보
        customMaterial = spriteRenderer.material;
    }

    void Update()
    {
        Sprite currentSprite = spriteRenderer.sprite;
        if (currentSprite == null || pixelSortShader == null) return;

        Texture2D currentTexture = currentSprite.texture;

        // 스프라이트 애니메이션 등으로 인해 텍스처 소스 크기나 해상도가 변경되었을 때 처리
        if (renderTexture == null || lastActiveTexture != currentTexture)
        {
            if (renderTexture != null) renderTexture.Release();

            renderTexture = new RenderTexture(currentTexture.width, currentTexture.height, 0, RenderTextureFormat.ARGB32);
            renderTexture.enableRandomWrite = true;
            renderTexture.Create();

            lastActiveTexture = currentTexture;
            
            // 커스텀 스프라이트 셰이더의 _SortedTex 슬롯에 타겟 전달
            customMaterial.SetTexture("_SortedTex", renderTexture);
        }

        // 1. 매 프레임 재생되는 스프라이트의 현재 원본 프레임을 RenderTexture에 덮어쓰기
        Graphics.Blit(currentTexture, renderTexture);

        int width = currentTexture.width;
        int height = currentTexture.height;

        // 2. 컴퓨트 셰이더 데이터 바인딩
        pixelSortShader.SetTexture(kernelIndex, "Texture", renderTexture);
        pixelSortShader.SetInt("Width", width);
        pixelSortShader.SetInt("Height", height);
        pixelSortShader.SetFloat("Threshold", threshold);
        pixelSortShader.SetInt("Vertical", verticalSort ? 1 : 0);

        int threadGroupsX = Mathf.CeilToInt(width / 16f);
        int threadGroupsY = Mathf.CeilToInt(height / 16f);

        // 3. 실시간 이미지이므로 매 프레임 루프를 강하게 돌려 즉각 정렬 완료 상태를 만듦
        for (int i = 0; i < iterationsPerFrame; i++)
        {
            pixelSortShader.SetInt("Stage", 0); // Even Pass
            pixelSortShader.Dispatch(kernelIndex, threadGroupsX, threadGroupsY, 1);

            pixelSortShader.SetInt("Stage", 1); // Odd Pass
            pixelSortShader.Dispatch(kernelIndex, threadGroupsX, threadGroupsY, 1);
        }
    }

    void OnDestroy()
    {
        if (renderTexture != null) renderTexture.Release();
    }
}