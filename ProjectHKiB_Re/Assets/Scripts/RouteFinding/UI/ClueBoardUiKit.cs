using TMPro;
using UnityEngine;

namespace RouteFinding.UI
{
    // 보드 패널 UI를 "프리팹 우선"으로 만드는 도우미.
    //
    // 규칙: 컨테이너·버튼·카드처럼 디자이너가 프리팹에서 고칠 수 있는 요소는 **이름으로 찾고 없을 때만 만든다**.
    // created가 true일 때만 코드가 기본 배치·외형을 채우고, 프리팹에 있던 것은 RectTransform·색·글자를 그대로 둔다.
    // 컴포넌트(Button/Image/TMP)는 없으면 붙이고, 런타임 콜백(onClick 등)은 직렬화되지 않으므로 언제나 다시 건다.
    // 노드 카드·연결선·힌트 말풍선·목록 항목처럼 데이터에 따라 바뀌는 것만 매번 새로 만든다.
    internal static class ClueBoardUiKit
    {
        /// <summary>직속 자식에서 name을 찾고 없으면 RectTransform 오브젝트를 만든다.</summary>
        public static RectTransform Child(Transform parent, string name, out bool created)
        {
            Transform found = parent != null ? parent.Find(name) : null;
            if (found != null)
            {
                created = false;
                var rect = found as RectTransform;
                return rect != null ? rect : found.gameObject.AddComponent<RectTransform>();
            }
            created = true;
            var go = new GameObject(name, typeof(RectTransform));
            if (parent != null) go.transform.SetParent(parent, false);
            return (RectTransform)go.transform;
        }

        public static RectTransform Child(Transform parent, string name) => Child(parent, name, out _);

        public static T Ensure<T>(GameObject go) where T : Component
        {
            T component = go.GetComponent<T>();
            return component != null ? component : go.AddComponent<T>();
        }

        /// <summary>
        /// TMP 텍스트를 보장한다. 새로 만든 오브젝트일 때만 폰트·크기·색·정렬·문구를 넣고, 프리팹에 있던 것은 그대로 둔다.
        /// 레이캐스트는 언제나 끈다(글자가 클릭을 가로채면 안 된다).
        /// </summary>
        public static TextMeshProUGUI Text(RectTransform rect, bool created, TMP_FontAsset font, float size, Color color,
            TextAlignmentOptions align, string text = null)
        {
            var tmp = Ensure<TextMeshProUGUI>(rect.gameObject);
            if (created)
            {
                if (font != null) tmp.font = font;
                tmp.fontSize = size;
                tmp.color = color;
                tmp.alignment = align;
                tmp.enableWordWrapping = false;
                if (text != null) tmp.text = text;
            }
            tmp.raycastTarget = false;
            return tmp;
        }

        public static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        /// <summary>동적 자식(노드·선·항목)을 전부 지운다. 에디터 검증에서도 돌므로 상황에 맞는 파괴를 고른다.</summary>
        public static void ClearChildren(Transform parent)
        {
            if (parent == null) return;
            for (int i = parent.childCount - 1; i >= 0; i--)
            {
                GameObject child = parent.GetChild(i).gameObject;
                if (Application.isPlaying) Object.Destroy(child);
                else Object.DestroyImmediate(child);
            }
        }
    }
}
