using UnityEngine;

// 단서 시스템의 콘텐츠 작업자용 전역 문구·설정. 코드 상수(기본값)를 덮어쓰는 한 장짜리 에셋이며,
// Resources/ClueSystemSettings.asset 하나만 읽는다. 없거나 항목이 비어 있으면 코드 기본값을 그대로 쓴다 —
// 에셋을 안 만들어도 모든 화면이 전과 똑같이 동작해야 한다.
//
// 편집은 맵 DB 편집기 "기타 설정" 탭에서 한다. 단서·보드·맵 데이터(JSON)와 달리 에셋이므로 그 탭의 저장은
// AssetDatabase.SaveAssets다.
[CreateAssetMenu(fileName = "ClueSystemSettings", menuName = "RouteFinding/단서 시스템 설정")]
public sealed class ClueSystemSettings : ScriptableObject
{
    public const string ResourcePath = "ClueSystemSettings";

    [Header("무관 연결 거절 코멘트")]
    [Tooltip("[A]/[B]가 시도 순서대로 단서 이름으로 바뀐다. (와/과)·(은/는)·(이/가)·(을/를)은 앞 글자 받침에 맞춰 골라진다. 비우면 코드 기본 문구.")]
    [TextArea(2, 4)] public string rejectionCommentTemplate = ClueBoardRejectionComment.DefaultTemplate;
    [Tooltip("거절 코멘트 카드 제목. 비우면 코드 기본값.")]
    public string rejectionCommentTitle = ClueBoardRejectionComment.DefaultTitle;

    [Header("연결 성공 코멘트")]
    [Tooltip("관계에 코멘트도 해몽 결과도 없을 때 연결 성공 시 보이는 문구. [A]/[B]는 관계의 첫/둘째 단서 이름. 비우면 코드 기본 문구.")]
    [TextArea(2, 4)] public string connectionCommentTemplate = ClueBoardConnectionComment.DefaultTemplate;

    [Header("실루엣 힌트")]
    [Tooltip("단서·슬롯 어디에도 실루엣 힌트가 없을 때 보이는 문구. 비우면 코드 기본 문구.")]
    [TextArea(2, 4)] public string defaultSilhouetteHint = ClueBoardSilhouetteHint.DefaultHint;

    private static ClueSystemSettings _cached;
    private static bool _searched;

    public static ClueSystemSettings Current
    {
        get
        {
            if (!_searched)
            {
                _cached = Resources.Load<ClueSystemSettings>(ResourcePath);
                _searched = true;
            }
            return _cached;
        }
    }

    /// <summary>검증/에디터용 — 캐시를 버려 다음 조회 때 다시 읽게 한다.</summary>
    public static void ClearCache()
    {
        _cached = null;
        _searched = false;
    }

    // 아래 정적 조회는 "에셋 값이 비어 있지 않으면 그것, 아니면 기본값" 규칙을 한 곳에 둔다.
    public static string RejectionTemplate =>
        Pick(Current?.rejectionCommentTemplate, ClueBoardRejectionComment.DefaultTemplate);

    public static string RejectionTitle =>
        Pick(Current?.rejectionCommentTitle, ClueBoardRejectionComment.DefaultTitle);

    public static string ConnectionTemplate =>
        Pick(Current?.connectionCommentTemplate, ClueBoardConnectionComment.DefaultTemplate);

    public static string SilhouetteHint =>
        Pick(Current?.defaultSilhouetteHint, ClueBoardSilhouetteHint.DefaultHint);

    private static string Pick(string value, string fallback) =>
        string.IsNullOrWhiteSpace(value) ? fallback : value;
}
