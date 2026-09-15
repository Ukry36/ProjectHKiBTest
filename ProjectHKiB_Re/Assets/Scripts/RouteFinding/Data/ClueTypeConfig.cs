using System.Collections.Generic;

// ClueType enum 값에 대한 표시 이름 정적 조회 테이블. EmotionColorConfig와 동일한 패턴.
public static class ClueTypeConfig
{
    private static readonly Dictionary<ClueType, string> Table = new()
    {
        { ClueType.Symbol,  "상징/글" },
        { ClueType.Picture, "그림/사진" },
        { ClueType.Prints,  "인쇄물" },
        { ClueType.Object,  "물체" },
    };

    // 필터 버튼 등 UI가 4종을 일정한 순서로 나열할 때 쓴다(enum 값 순).
    public static readonly IReadOnlyList<ClueType> AllTypes = new[]
        { ClueType.Symbol, ClueType.Picture, ClueType.Prints, ClueType.Object };

    public static bool IsKnown(ClueType type) => Table.ContainsKey(type);

    public static string GetDisplayName(ClueType type) =>
        Table.TryGetValue(type, out var name) ? name : "";

    public static string GetDisplayName(ClueClassification classification) =>
        IsValid(classification) ? GetDisplayName(classification.type) : "";

    public static bool IsValid(ClueClassification classification) =>
        classification != null && GetParent(classification.subtype) == classification.type &&
        Table.ContainsKey(classification.type);

    public static ClueType GetParent(ClueSubtype subtype)
    {
        switch (subtype)
        {
            case ClueSubtype.Symbol:
            case ClueSubtype.ObscureText:
            case ClueSubtype.TextFragment:
            case ClueSubtype.DamagedText: return ClueType.Symbol;
            case ClueSubtype.Drawing:
            case ClueSubtype.Photograph: return ClueType.Picture;
            case ClueSubtype.PromotionalMaterial:
            case ClueSubtype.Flyer:
            case ClueSubtype.Newspaper: return ClueType.Prints;
            case ClueSubtype.PhysicalObject: return ClueType.Object;
            default: return (ClueType)0;
        }
    }

    public static string GetSubtypeName(ClueSubtype subtype)
    {
        switch (subtype)
        {
            case ClueSubtype.Symbol: return "상징";
            case ClueSubtype.ObscureText: return "난해한 글";
            case ClueSubtype.TextFragment: return "조각글";
            case ClueSubtype.DamagedText: return "훼손글";
            case ClueSubtype.Drawing: return "그림";
            case ClueSubtype.Photograph: return "사진";
            case ClueSubtype.PromotionalMaterial: return "홍보물";
            case ClueSubtype.Flyer: return "전단지";
            case ClueSubtype.Newspaper: return "신문";
            case ClueSubtype.PhysicalObject: return "물체";
            default: return "미분류";
        }
    }
}
