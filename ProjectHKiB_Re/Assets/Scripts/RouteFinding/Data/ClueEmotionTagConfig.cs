using System.Collections.Generic;
using UnityEngine;

// ClueEmotionTag의 표시 이름·색 정적 조회 테이블. ClueTypeConfig와 동일한 패턴.
//
// [전투 감정과 무관하다] 이 표는 전투 `EmotionColor`를 전혀 참조하지 않는다. 단서 감정 4종과
// 전투 감정 12종은 독립 체계이며(감정 개편 계획 E06), 둘 사이에 캐스팅이나 색 공유를 만들면
// 한쪽 밸런스를 고칠 때 다른 쪽 표시가 조용히 따라 바뀐다.
//
// [네 번째 감정] `ReservedFourth`는 콘텐츠가 확정되기 전의 자리 예약이다. 실제 색을 주면 화면에
// 그럴듯하게 나타나 "이미 정해진 감정"처럼 보이므로, 색 조회는 실패시키고 호출부가 중립색으로
// 떨어지게 한다(C01 저장 검증도 이 값을 거절한다).
public static class ClueEmotionTagConfig
{
    // 감정이 없거나(Unset) 아직 정하지 않은(ReservedFourth) 선에 쓰는 색.
    public static readonly Color NeutralLineColor = new(0.62f, 0.66f, 0.72f, 0.85f);

    // 필터 버튼이 나열하는 순서. 확정 3종 뒤에 예약값을 두어 "네 번째 자리"가 화면에 남되 눌리지는 않게 한다.
    public static readonly IReadOnlyList<ClueEmotionTag> FilterOrder = new[]
        { ClueEmotionTag.Joy, ClueEmotionTag.Sadness, ClueEmotionTag.Anger, ClueEmotionTag.ReservedFourth };

    /// <summary>플레이어가 고를 수 있는(콘텐츠가 확정된) 감정인지. Unset과 ReservedFourth는 false.</summary>
    public static bool IsSelectable(ClueEmotionTag tag) =>
        tag == ClueEmotionTag.Joy || tag == ClueEmotionTag.Sadness || tag == ClueEmotionTag.Anger;

    public static string GetDisplayName(ClueEmotionTag tag) => tag switch
    {
        ClueEmotionTag.Joy            => "기쁨",
        ClueEmotionTag.Sadness        => "슬픔",
        ClueEmotionTag.Anger          => "분노",
        ClueEmotionTag.ReservedFourth => "(미정)",
        _                             => "(없음)",
    };

    /// <summary>
    /// 실제 화면에 쓸 색이 정해진 감정인지. Unset과 ReservedFourth는 false를 돌려주고,
    /// 호출부는 NeutralLineColor로 떨어진다.
    /// </summary>
    public static bool TryGetLineColor(ClueEmotionTag tag, out Color color)
    {
        switch (tag)
        {
            case ClueEmotionTag.Joy:     color = new Color(0.95f, 0.82f, 0.35f, 0.95f); return true;
            case ClueEmotionTag.Sadness: color = new Color(0.40f, 0.62f, 0.90f, 0.95f); return true;
            case ClueEmotionTag.Anger:   color = new Color(0.90f, 0.42f, 0.35f, 0.95f); return true;
            default:                     color = NeutralLineColor; return false;
        }
    }

    /// <summary>
    /// 연결선 하나의 색. 양 끝 단서의 감정이 **같고** 그 감정에 확정 색이 있을 때만 그 색을 쓰고,
    /// 그 밖에는 중립색이다.
    ///
    /// 서로 다른 감정을 섞어 새 색을 만드는 규칙(기획 PDF p.4의 보라/검정/흰색)은 원문에도
    /// "임시 설정"으로 적혀 있고 전투 감정 합성과 혼동되기 쉬워, 확정 전까지 구현하지 않는다 —
    /// 임의 조합색을 넣으면 나중에 실제 규칙이 정해졌을 때 이미 만들어진 화면이 근거처럼 쓰인다.
    /// </summary>
    public static Color ResolveLineColor(ClueEmotionTag first, ClueEmotionTag second)
    {
        if (first != second) return NeutralLineColor;
        return TryGetLineColor(first, out Color color) ? color : NeutralLineColor;
    }
}
