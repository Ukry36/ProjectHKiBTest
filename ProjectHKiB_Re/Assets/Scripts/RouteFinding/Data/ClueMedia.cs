using System;

// 단서 본문을 이루는 매체 블록의 종류.
// JSON에는 정수(enum index)로 저장되므로 기존 값의 순서를 바꾸지 말 것 — 뒤에만 추가한다.
//
// [분류와 무관하다] 이 enum은 "무엇으로 보여주는가"(매체 형식)만 정한다. 단서의 의미 분류
// (ClueType: Symbol/Picture/Prints/Object)와는 독립이며, 영상/오디오를 ClueType으로 추가하지
// 않는다는 결정(격차 분석 6-8)을 코드로 지킨 형태다 — 사진으로 찍은 신문(Prints + Image)처럼
// 두 축이 자유롭게 조합될 수 있다.
public enum ClueMediaKind
{
    Text = 0,   // 글 — text 필드를 그대로 카드에 출력
    Image = 1,  // 사진/그림 — Addressable 주소의 Sprite(또는 Texture2D)
    Video = 2,  // 영상 — Addressable 주소의 VideoClip
    Audio = 3   // 소리 — Addressable 주소의 AudioClip
}

// 단서 본문 한 블록. ClueData.mediaBlocks에 배열로 담기고 clues.json에 같이 저장된다.
//
// 첨부물(ClueAttachment)과 다른 점: 첨부물은 "카드 아래에 덧붙는 자료 목록"이고, 이쪽은 본문
// 자체다 — 글/사진/영상/소리를 작성자가 정한 순서대로 이어 붙여 한 편의 본문을 만든다.
// 실제 에셋을 JSON에 담을 수 없어 Addressable 주소로 참조하는 방식(ClueAttachment.cs 상단 주석)은
// 그대로 재사용하며, 로딩도 같은 ClueAttachmentService를 쓴다.
//
// 기존 문자열 본문(ClueData.content)은 그대로 남는다 — mediaBlocks가 비어 있는 단서는 지금까지와
// 똑같이 content만 보여준다(C01: "기존 문자열 본문과 attachments를 보존").
[Serializable]
public class ClueMediaBlock
{
    public ClueMediaKind kind;
    public string text;     // Text 전용 — 실제 본문 문장
    public string address;  // Image/Video/Audio 전용 — Addressable 주소
    public string caption;  // 매체 아래에 붙는 설명(선택). Text 블록에서는 쓰지 않는다
}

// ClueMediaKind 표시 이름 정적 조회 테이블. ClueAttachmentConfig와 동일한 패턴.
public static class ClueMediaConfig
{
    public static string GetDisplayName(ClueMediaKind kind) => kind switch
    {
        ClueMediaKind.Text  => "글",
        ClueMediaKind.Image => "사진",
        ClueMediaKind.Video => "영상",
        ClueMediaKind.Audio => "소리",
        _                   => kind.ToString(),
    };

    // Text만 본문 문자열을 쓰고 나머지는 주소를 쓴다 — 검증·편집기·표시가 같은 기준을 공유하도록
    // 여기 한 곳에만 둔다(둘로 나뉘면 "주소가 필요한 종류"의 정의가 서로 어긋날 수 있다).
    public static bool NeedsAddress(ClueMediaKind kind) => kind != ClueMediaKind.Text;
}
