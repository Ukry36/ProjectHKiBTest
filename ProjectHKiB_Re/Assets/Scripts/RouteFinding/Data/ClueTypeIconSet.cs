using UnityEngine;

// 단서 유형(Symbol/Picture/Prints/Object)별 기본 아이콘. 단서마다 대표 아이콘(ClueData.iconAddress)을 지정하지
// 않았을 때 획득 토스트·보드 노드가 이 유형 아이콘으로 떨어진다. 단서별 아이콘이 있으면 그것이 언제나 우선이다 —
// 유형 아이콘은 "이 단서가 무엇인지"가 아니라 "어떤 종류인지"만 말해 주는 대체품이라서다.
//
// 에셋은 Resources/ClueTypeIcons.asset 하나다(Assets > Create > RouteFinding > 단서 유형 아이콘). 없으면 아이콘 없음으로
// 동작하며 어떤 화면도 막지 않는다.
[CreateAssetMenu(fileName = "ClueTypeIcons", menuName = "RouteFinding/단서 유형 아이콘")]
public sealed class ClueTypeIconSet : ScriptableObject
{
    public const string ResourcePath = "ClueTypeIcons";

    [Header("유형별 기본 아이콘 (비우면 그 유형은 아이콘 없음)")]
    public Sprite symbol;   // 상징/글
    public Sprite picture;  // 그림/사진
    public Sprite prints;   // 인쇄물
    public Sprite physical; // 물체

    private static ClueTypeIconSet _cached;
    private static bool _searched;

    public static ClueTypeIconSet Current
    {
        get
        {
            if (!_searched)
            {
                _cached = Resources.Load<ClueTypeIconSet>(ResourcePath);
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

    public Sprite Get(ClueType type) => type switch
    {
        ClueType.Symbol => symbol,
        ClueType.Picture => picture,
        ClueType.Prints => prints,
        ClueType.Object => physical,
        _ => null,
    };

    /// <summary>
    /// 단서의 표시 아이콘: 단서별 대표 아이콘(iconAddress) → 유형 기본 아이콘 → null.
    /// 미분류(classification 없음)는 유형 아이콘도 없다.
    /// </summary>
    public static Sprite ResolveIcon(ClueData clue)
    {
        if (clue == null) return null;
        Sprite own = ClueAttachmentService.LoadSprite(clue.iconAddress);
        if (own != null) return own;
        ClueTypeIconSet set = Current;
        if (set == null || !ClueTypeConfig.IsValid(clue.classification)) return null;
        return set.Get(clue.classification.type);
    }
}
