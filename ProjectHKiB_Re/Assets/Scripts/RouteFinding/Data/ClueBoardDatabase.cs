using System;

// 단서 보드 정의 파일(Resources/clue_boards.json)의 최상위 컨테이너.
//
// [왜 clues.json과 분리하나] 보드는 "어떤 단서가 어느 자리에 놓이고 무엇과 이어지는가"를 정하고,
// clues.json은 "그 단서가 무엇인가"를 정한다. 한 파일에 합치면 보드 레이아웃을 손볼 때마다 단서
// 본문까지 통째로 다시 쓰게 되어, C01이 만든 단서 저장 보호(원본 바이트 비교 → 백업 → File.Replace)의
// 보호 범위가 필요 이상으로 넓어진다. 파일을 나누면 두 저장이 서로를 덮어쓸 일도 없다.
//
// [ClueBoardDefinition과의 관계] 보드 한 장의 내용(슬롯/관계/실루엣/초기 연결)은 전부
// ClueBoardDefinition에 있다. 이 클래스는 그 배열과 스키마 버전만 얹는 봉투다 — 판정 계약
// (ClueBoardConnectionEngine)은 이 컨테이너를 몰라도 되게 유지한다.
[Serializable]
public class ClueBoardDatabase
{
    // 1 = 최초 형식(고정 슬롯 보드). 버전 없는 파일은 "구형/버전 누락"으로 거절한다 —
    // 새로 만드는 형식이라 조용히 승격시킬 구 데이터가 없고, 버전을 빠뜨린 파일을 통과시키면
    // 나중에 진짜 구형이 생겼을 때 둘을 구분할 방법이 사라진다(ClueBoardDatabaseCodec 참고).
    public const int CurrentSchemaVersion = 1;

    public int schemaVersion;
    public ClueBoardDefinition[] boards = Array.Empty<ClueBoardDefinition>();
}
