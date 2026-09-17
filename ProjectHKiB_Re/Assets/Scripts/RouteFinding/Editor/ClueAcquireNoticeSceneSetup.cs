using System.Linq;
using RouteFinding.UI;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace RouteFinding.Editor
{
    /// <summary>기존 보드 프리팹을 건드리지 않고, 현재 씬 Canvas에 독립 단서 획득 토스트를 추가합니다.</summary>
    public static class ClueAcquireNoticeSceneSetup
    {
        private const string ObjectName = "ClueAcquireNotice";

        [MenuItem("Tools/RouteFinding/단서 획득 알림 씬 세팅")]
        public static void AddToOpenScene()
        {
            Scene scene = SceneManager.GetActiveScene();
            if (!scene.IsValid() || string.IsNullOrEmpty(scene.path))
            {
                EditorUtility.DisplayDialog("단서 획득 알림", "저장된 씬을 연 뒤 다시 실행해 주세요.", "확인");
                return;
            }

            GameObject existing = scene.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<ClueAcquireNotice>(true))
                .Select(notice => notice.gameObject).FirstOrDefault(x => x.name == ObjectName);
            if (existing != null)
            {
                // 치수를 바꾼 뒤 다시 실행하면 옛 오브젝트를 지우고 새로 만든다(인스펙터 설정은 초기화된다).
                if (!EditorUtility.DisplayDialog("단서 획득 알림",
                        "이미 씬에 있습니다. 지우고 현재 치수로 다시 만들까요?\n(폰트·효과음 등 인스펙터 설정은 초기화됩니다)",
                        "다시 만들기", "그대로 두기"))
                {
                    Selection.activeGameObject = existing;
                    EditorGUIUtility.PingObject(existing);
                    return;
                }
                Undo.DestroyObjectImmediate(existing);
            }

            Canvas canvas = FindHostCanvas(scene);
            if (canvas == null)
            {
                EditorUtility.DisplayDialog("단서 획득 알림", "GraphicRaycaster가 있는 루트 Canvas를 먼저 배치해 주세요.", "확인");
                return;
            }

            var host = new GameObject(ObjectName, typeof(RectTransform));
            Undo.RegisterCreatedObjectUndo(host, "단서 획득 알림 배치");
            Parent(host.transform, canvas.transform);
            var hostRect = (RectTransform)host.transform;
            hostRect.anchorMin = Vector2.zero; hostRect.anchorMax = Vector2.one;
            hostRect.offsetMin = hostRect.offsetMax = Vector2.zero;

            var visual = new GameObject("Toast", typeof(RectTransform), typeof(Image), typeof(CanvasGroup));
            Undo.RegisterCreatedObjectUndo(visual, "단서 획득 알림 배치");
            Parent(visual.transform, host.transform);
            var rect = (RectTransform)visual.transform;
            rect.anchorMin = rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(1f, 1f);
            // 치수는 다른 UI와 같은 캔버스 단위 기준이다 — 보드 320×240, 도감 상단바 12, 제목 글자 8pt, 버튼 7pt.
            // 처음 값(330×92, 22pt)은 보드 한 장보다 커서 카메라를 덮었다. 도감 상단바 두 줄 높이 정도로 맞춘다.
            rect.anchoredPosition = new Vector2(-8f, -8f);
            rect.sizeDelta = new Vector2(104f, 26f);
            visual.GetComponent<Image>().color = new Color(.08f, .08f, .1f, .92f);

            Image icon = CreateImage("Icon", visual.transform, new Vector2(4f, 0f), new Vector2(18f, 18f));
            icon.preserveAspect = true;
            var title = CreateText("Title", visual.transform, new Vector2(26f, 3.5f), 7f, "단서 이름");
            var message = CreateText("Message", visual.transform, new Vector2(26f, -4f), 5.5f, "단서 획득");
            message.color = new Color(.72f, .76f, .82f);
            TMP_FontAsset font = FindProjectFont();
            if (font != null) { title.font = font; message.font = font; }

            var notice = Undo.AddComponent<ClueAcquireNotice>(host);
            var serialized = new SerializedObject(notice);
            serialized.FindProperty("_noticeRoot").objectReferenceValue = rect;
            serialized.FindProperty("_canvasGroup").objectReferenceValue = visual.GetComponent<CanvasGroup>();
            serialized.FindProperty("_icon").objectReferenceValue = icon;
            serialized.FindProperty("_title").objectReferenceValue = title;
            serialized.FindProperty("_message").objectReferenceValue = message;
            serialized.FindProperty("_font").objectReferenceValue = font;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.MarkSceneDirty(scene);
            Selection.activeGameObject = host;
            EditorGUIUtility.PingObject(host);
        }

        private static Image CreateImage(string name, Transform parent, Vector2 anchoredPosition, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            Undo.RegisterCreatedObjectUndo(go, "단서 획득 알림 배치");
            Parent(go.transform, parent);
            var rect = (RectTransform)go.transform;
            rect.anchorMin = rect.anchorMax = new Vector2(0f, .5f);
            rect.pivot = new Vector2(0f, .5f); rect.anchoredPosition = anchoredPosition; rect.sizeDelta = size;
            return go.GetComponent<Image>();
        }

        private static TextMeshProUGUI CreateText(string name, Transform parent, Vector2 anchoredPosition, float size, string value)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            Undo.RegisterCreatedObjectUndo(go, "단서 획득 알림 배치");
            Parent(go.transform, parent);
            var rect = (RectTransform)go.transform;
            rect.anchorMin = rect.anchorMax = new Vector2(0f, .5f);
            rect.pivot = new Vector2(0f, .5f); rect.anchoredPosition = anchoredPosition; rect.sizeDelta = new Vector2(74f, 10f);
            var text = go.GetComponent<TextMeshProUGUI>();
            text.fontSize = size; text.text = value; text.alignment = TextAlignmentOptions.MidlineLeft;
            text.enableWordWrapping = false; text.overflowMode = TextOverflowModes.Ellipsis; text.raycastTarget = false;
            return text;
        }

        // Undo.SetTransformParent는 월드 위치를 유지한 채 붙인다. 캔버스는 Screen Space-Camera라 월드에서 1/16 배율·
        // 카메라 앞 거리로 놓여 있으므로, 원점에 만든 새 오브젝트를 그대로 붙이면 로컬 스케일 16·z -1440처럼 튀어
        // 화면 밖으로 나간다(처음 배치가 "카메라에서 안 보임"이던 원인). 붙인 뒤 로컬 변환을 항상 초기화한다.
        private static void Parent(Transform child, Transform parent)
        {
            Undo.SetTransformParent(child, parent, "단서 획득 알림 배치");
            child.localPosition = Vector3.zero;
            child.localRotation = Quaternion.identity;
            child.localScale = Vector3.one;
        }

        private static Canvas FindHostCanvas(Scene scene) => scene.GetRootGameObjects()
            .SelectMany(root => root.GetComponentsInChildren<Canvas>(true))
            .Where(canvas => canvas.transform.parent == null && canvas.GetComponent<GraphicRaycaster>() != null)
            .OrderByDescending(canvas => canvas.sortingOrder).FirstOrDefault();

        // 프로젝트 공통 픽셀 폰트(neodgm)를 우선 쓴다. 다른 패널의 인스펙터 폰트도 이것이라 "아무 TMP 폰트나"
        // 집으면 토스트만 글꼴이 달라진다. 없을 때만 씬의 다른 텍스트 → 아무 TMP 폰트 순으로 떨어진다.
        private const string PreferredFontPath = "Assets/Font/neodgm.asset";

        private static TMP_FontAsset FindProjectFont()
        {
            var preferred = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(PreferredFontPath);
            if (preferred != null) return preferred;
            Debug.LogWarning($"[ClueAcquireNoticeSceneSetup] {PreferredFontPath}를 찾지 못해 다른 폰트로 대체합니다.");
            TextMeshProUGUI sample = Object.FindObjectsOfType<TextMeshProUGUI>(true).FirstOrDefault(x => x.font != null);
            if (sample != null) return sample.font;
            string guid = AssetDatabase.FindAssets("t:TMP_FontAsset").FirstOrDefault();
            return string.IsNullOrEmpty(guid) ? null : AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(AssetDatabase.GUIDToAssetPath(guid));
        }
    }
}
