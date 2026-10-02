using UnityEngine;
using UnityEngine.UI;

namespace RouteFinding.UI
{
    /// <summary>
    /// RouteFinding UI가 런타임으로 계층을 만들 때 쓰는 uGUI 기본 동작 모음.
    ///
    /// 원래 이 네 개는 도감·노트·인터넷·지도·단서 보드의 뷰 클래스마다 private static으로 복사돼
    /// 있었다(파일에 따라 널 가드 유무만 달랐다). 같은 일을 하는 코드가 스무 군데로 갈라져 있으면
    /// 한쪽만 고쳐지므로 여기로 모으고, 각 파일은 `using static RouteFinding.UI.RouteUiKit;`으로 쓴다.
    /// 합칠 때는 널 가드가 있는 쪽을 택했다 — 없던 파일에서도 동작이 달라지지 않는다.
    ///
    /// [ClueBoardUiKit과 구분할 것] 이쪽은 "늘 새로 만든다". 저쪽은 프리팹에 이미 있는 자식을
    /// 찾아 재사용하는 멱등 API라(디자인 보존), 목적이 다르다.
    /// </summary>
    public static class RouteUiKit
    {
        /// <summary>RectTransform을 가진 빈 오브젝트를 만들어 parent 밑에 붙인다(parent가 null이면 루트).</summary>
        public static RectTransform NewRect(Transform parent, string name)
        {
            var go = new GameObject(name, typeof(RectTransform));
            if (parent != null) go.transform.SetParent(parent, false);
            return (RectTransform)go.transform;
        }

        /// <summary>부모를 가득 채우도록 앵커와 여백을 맞춘다.</summary>
        public static void StretchFull(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
        }

        /// <summary>단색 Image를 붙여 돌려준다.</summary>
        public static Image AddImg(RectTransform rt, Color col)
        {
            var img = rt.gameObject.AddComponent<Image>();
            img.color = col;
            return img;
        }

        /// <summary>이름으로 자손을 깊이 우선 탐색한다. 비활성 오브젝트도 찾는다.</summary>
        public static Transform FindDeepTransform(Transform parent, string childName)
        {
            if (parent == null) return null;
            foreach (Transform child in parent)
            {
                if (child.name == childName) return child;
                Transform found = FindDeepTransform(child, childName);
                if (found != null) return found;
            }
            return null;
        }
    }
}
