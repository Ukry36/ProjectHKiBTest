using System;
using System.Reflection;
using RouteFinding.UI;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace RouteFinding.Editor.Validation
{
    /// <summary>실제 Canvas 계층에서 토스트가 아이콘 누락·큐 상한을 안전하게 처리하는지 확인합니다.</summary>
    public static class AcquireNoticeValidation
    {
        [MenuItem("Tools/RouteFinding/Validation/C05 획득 토스트 회귀 검증")]
        public static void Run()
        {
            int pass = 0;
            GameObject canvasObject = new GameObject("AcquireNoticeValidationCanvas", typeof(Canvas));
            GameObject host = new GameObject("AcquireNoticeValidationHost", typeof(RectTransform), typeof(ClueAcquireNotice));
            GameObject visual = new GameObject("Toast", typeof(RectTransform), typeof(CanvasGroup));
            GameObject iconObject = new GameObject("Icon", typeof(RectTransform), typeof(Image));
            GameObject titleObject = new GameObject("Title", typeof(RectTransform), typeof(TextMeshProUGUI));
            GameObject messageObject = new GameObject("Message", typeof(RectTransform), typeof(TextMeshProUGUI));
            try
            {
                host.transform.SetParent(canvasObject.transform, false);
                visual.transform.SetParent(host.transform, false);
                iconObject.transform.SetParent(visual.transform, false);
                titleObject.transform.SetParent(visual.transform, false);
                messageObject.transform.SetParent(visual.transform, false);
                var notice = host.GetComponent<ClueAcquireNotice>();
                var so = new SerializedObject(notice);
                so.FindProperty("_noticeRoot").objectReferenceValue = visual.GetComponent<RectTransform>();
                so.FindProperty("_canvasGroup").objectReferenceValue = visual.GetComponent<CanvasGroup>();
                so.FindProperty("_icon").objectReferenceValue = iconObject.GetComponent<Image>();
                so.FindProperty("_title").objectReferenceValue = titleObject.GetComponent<TextMeshProUGUI>();
                so.FindProperty("_message").objectReferenceValue = messageObject.GetComponent<TextMeshProUGUI>();
                so.FindProperty("_maxQueueLength").intValue = 2;
                so.ApplyModifiedPropertiesWithoutUndo();

                Invoke(notice, "Awake");
                Invoke(notice, "ApplyClue", new ClueData { id = "preview", name = "미리보기", iconAddress = "" });
                Invoke(notice, "HandleClueAcquired", new ClueData { id = "a", name = "첫 단서", iconAddress = "" });
                Invoke(notice, "HandleClueAcquired", new ClueData { id = "b", name = "둘째 단서", iconAddress = "" });
                // StartCoroutine은 첫 yield까지 즉시 실행되므로 첫 단서는 바로 화면에 올라가고 둘째만 큐에 남는다.
                if (notice.PendingCount != 1) throw new Exception("queue length did not retain sequential notices");
                if (titleObject.GetComponent<TextMeshProUGUI>().text != "첫 단서") throw new Exception("first notice was not displayed first");
                if (messageObject.GetComponent<TextMeshProUGUI>().text != "단서 획득") throw new Exception("toast text missing");
                if (iconObject.activeSelf) throw new Exception("missing icon did not collapse its slot");
                pass += 4;
                Debug.Log($"[AcquireNoticeValidation] PASS {pass}/4 (Canvas queue and missing-icon UI)");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(canvasObject);
            }
        }

        private static void Invoke(ClueAcquireNotice target, string name, params object[] args)
        {
            MethodInfo method = typeof(ClueAcquireNotice).GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic);
            if (method == null) throw new MissingMethodException(typeof(ClueAcquireNotice).Name, name);
            method.Invoke(target, args);
        }
    }
}
