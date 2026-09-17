using UnityEngine;

// UIManager.windows에 등록된 창 이름을 담는 string 필드에 붙인다. 인스펙터(이벤트 체인 편집기 포함)에서
// 등록된 이름 드롭다운 또는 하이라키의 Window 오브젝트를 끌어다 놓아 고를 수 있다(WindowNameDrawer).
// 런타임 동작은 바뀌지 않는다 — 값은 여전히 문자열이고 UIManager가 그 이름으로 찾는다.
public class WindowNameAttribute : PropertyAttribute { }
