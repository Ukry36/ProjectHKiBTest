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
    // C03-Host 개발용: 지금 열려 있는 씬에 ClueBoardPanel을 하나 얹는다.
    //
    // [왜 도구로 만드나] 씬 파일(.unity)을 손으로 고치면 fileID·참조가 어긋나기 쉽다. 이 도구는
    // 유니티가 직접 직렬화하게 해서 **기존 하이라키는 그대로 두고 오브젝트 하나만 더한다.**
    //
    // [되돌리기] 배치는 Undo에 기록되므로 Ctrl+Z로 즉시 취소된다. 저장까지 했다면
    // `git checkout -- <씬 경로>`로 되돌릴 수 있다(씬을 커밋해 둔 경우).
    //
    // 정식 배선(UIManager 등록 + Input Action)이 들어오면 이 파일과 패널의 개발용 필드를 함께 지운다.
    public static class ClueBoardPanelSceneSetup
    {
        private const string FixturePath = "Assets/Tests/RouteFinding/ClueBoardHost/clue_boards.dev.json";
        private const string ObjectName = "ClueBoardPanel (개발용)";

        // 텍스트 3종과 사진/음성 단서를 해금하고, n4는 잠김, n5는 n1이 공개하는 실루엣으로 둔다.
        private static readonly string[] AcquiredClueIds =
        {
            "clue-forest-entrance", "clue-lakeside", "clue-forest-cabin",
            "clue-web-graffiti-photo", "clue-web-night-sound",
        };

        [MenuItem("Tools/RouteFinding/개발용/현재 씬에 단서 보드 패널 배치")]
        public static void AddToOpenScene()
        {
            Scene scene = SceneManager.GetActiveScene();
            if (!scene.IsValid() || string.IsNullOrEmpty(scene.path))
            {
                EditorUtility.DisplayDialog("단서 보드 패널",
                    "먼저 배치할 씬을 열어주세요(저장된 씬이어야 합니다).", "확인");
                return;
            }

            GameObject existing = scene.GetRootGameObjects().FirstOrDefault(go => go.name == ObjectName);
            if (existing != null)
            {
                // 이미 있으면 새로 만들지 않는다 — 같은 패널이 둘이면 어느 쪽이 열렸는지 알 수 없다.
                Selection.activeGameObject = existing;
                EditorGUIUtility.PingObject(existing);
                Debug.Log($"[ClueBoardPanelSceneSetup] '{ObjectName}'이(가) 이미 '{scene.name}'에 있습니다. 선택만 했습니다.");
                return;
            }

            var host = new GameObject(ObjectName, typeof(RectTransform));
            // Undo에 등록해 Ctrl+Z 한 번으로 배치 전으로 되돌아갈 수 있게 한다.
            Undo.RegisterCreatedObjectUndo(host, "단서 보드 패널 배치");

            // 기존 Canvas가 있으면 그 아래에 넣는다(런타임에 패널이 Canvas를 새로 만들지 않도록).
            // 없으면 루트에 두고, 실행 시 패널이 개발용 Canvas를 만들며 로그를 남긴다.
            Canvas canvas = FindHostCanvas(scene);
            if (canvas != null)
            {
                Undo.SetTransformParent(host.transform, canvas.transform, "단서 보드 패널 배치");
                var rect = (RectTransform)host.transform;
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.one;
                rect.offsetMin = Vector2.zero;
                rect.offsetMax = Vector2.zero;
                rect.localScale = Vector3.one;
            }

            var panel = Undo.AddComponent<ClueBoardPanel>(host);
            ConfigureForDevelopment(panel);

            EditorSceneManager.MarkSceneDirty(scene);
            Selection.activeGameObject = host;
            EditorGUIUtility.PingObject(host);

            Debug.Log($"[ClueBoardPanelSceneSetup] '{scene.name}'에 '{ObjectName}'을(를) 배치했습니다" +
                      (canvas != null ? $" (Canvas '{canvas.name}' 아래)." : " (루트 — 실행 시 개발용 Canvas가 만들어집니다).") +
                      " 기존 하이라키는 그대로입니다. 저장하지 않으면 반영되지 않으며, Ctrl+Z로 취소할 수 있습니다.");
        }

        // 인스펙터에서 손으로 채워야 하는 개발용 값을 미리 넣어 둔다 — 그래야 배치 직후 Play만 눌러도
        // 잠김/실루엣/해금이 한 화면에 나온다. 운영 콘텐츠는 건드리지 않는다(픽스처는 Resources 밖).
        private static void ConfigureForDevelopment(ClueBoardPanel panel)
        {
            var serialized = new SerializedObject(panel);
            serialized.FindProperty("_developmentControls").boolValue = true;
            serialized.FindProperty("_developmentLocalBoardId").stringValue = "dev-local-forest";
            serialized.FindProperty("_globalBoardId").stringValue = "dev-global";
            serialized.FindProperty("_developmentBoardDefinition").objectReferenceValue =
                AssetDatabase.LoadAssetAtPath<TextAsset>(FixturePath);

            SerializedProperty acquired = serialized.FindProperty("_developmentAcquiredClueIds");
            acquired.arraySize = AcquiredClueIds.Length;
            for (int i = 0; i < AcquiredClueIds.Length; i++)
                acquired.GetArrayElementAtIndex(i).stringValue = AcquiredClueIds[i];

            // 한글 단서 이름이 보이도록 프로젝트에 이미 있는 TMP 폰트를 하나 골라 넣는다.
            // 못 찾으면 비워 둔다 — 폰트가 없어도 노드/선/드래그는 그대로 동작한다.
            TMP_FontAsset font = FindProjectFont();
            if (font != null) serialized.FindProperty("_font").objectReferenceValue = font;

            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        // 씬에서 이미 쓰고 있는 Canvas를 고른다 — 새 Canvas를 만들면 기존 UI와 그리는 순서가 겹친다.
        private static Canvas FindHostCanvas(Scene scene)
        {
            Canvas best = null;
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                foreach (Canvas candidate in root.GetComponentsInChildren<Canvas>(true))
                {
                    if (candidate.transform.parent != null) continue; // 루트 Canvas만
                    if (candidate.GetComponent<GraphicRaycaster>() == null) continue; // 입력을 받는 것만
                    if (best == null || candidate.sortingOrder > best.sortingOrder) best = candidate;
                }
            }
            return best;
        }

        // 기존 UI가 쓰는 폰트를 그대로 빌려온다. 특정 에셋 이름에 의존하지 않으려고 씬에서 먼저 찾고,
        // 없으면 프로젝트 전체에서 아무 TMP 폰트나 고른다.
        private static TMP_FontAsset FindProjectFont()
        {
            TextMeshProUGUI sample = Object.FindObjectsOfType<TextMeshProUGUI>(true)
                .FirstOrDefault(text => text.font != null);
            if (sample != null) return sample.font;

            string guid = AssetDatabase.FindAssets("t:TMP_FontAsset").FirstOrDefault();
            return string.IsNullOrEmpty(guid)
                ? null
                : AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(AssetDatabase.GUIDToAssetPath(guid));
        }
    }
}
