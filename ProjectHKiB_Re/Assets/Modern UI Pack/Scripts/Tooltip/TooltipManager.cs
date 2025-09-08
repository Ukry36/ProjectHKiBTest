using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using DG.Tweening;



#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace Michsky.MUIP
{
    public class TooltipManager : MonoBehaviour
    {
        // Resources
        public Canvas mainCanvas;
        public Tooltip currentTooltip;
        public Camera targetCamera;

        // Settings
        [Range(0.01f, 0.5f)] public float tooltipSmoothness = 0.1f;
        [Range(5, 10)] public float dampSpeed = 10;
        public bool allowUpdate = false;
        public bool checkDispose = true;
        public CameraSource cameraSource = CameraSource.Main;
        public TransitionMode transitionMode = TransitionMode.Damp;

        // Content Bounds
        [Range(-50, 50)] public int vBorderTop = -15;
        [Range(-50, 50)] public int vBorderBottom = 10;
        [Range(-50, 50)] public int hBorderLeft = 20;
        [Range(-50, 50)] public int hBorderRight = -15;

        [HideInInspector] public TooltipContent currentContent;
        private RectTransform sourceRect;
        Vector3 contentPos = new Vector3(0, 0, 0);
        Vector3 tooltipVelocity = Vector3.zero;

        public enum CameraSource { Main, Custom }
        public enum TransitionMode { Damp, Snap }

        void Awake()
        {
            sourceRect = gameObject.GetComponent<RectTransform>();

            sourceRect.anchorMin = new Vector2(0, 0);
            sourceRect.anchorMax = new Vector2(1, 1);
            sourceRect.offsetMin = new Vector2(0, 0);
            sourceRect.offsetMax = new Vector2(0, 0);
            Tooltip[] tooltips = GetComponentsInChildren<Tooltip>();
            for (int i = 0; i < tooltips.Length; i++)
            {
                tooltips[i].rect = tooltips[i].GetComponent<RectTransform>();
            }

            if (mainCanvas == null) { mainCanvas = gameObject.GetComponentInParent<Canvas>(); }
            if (cameraSource == CameraSource.Main) { targetCamera = Camera.main; }

            contentPos = new Vector3(vBorderTop, hBorderLeft, 0);
            gameObject.transform.SetAsLastSibling();
        }

        void Update()
        {
            if (!allowUpdate) return;
            if (checkDispose && currentContent != null && !currentContent.gameObject.activeInHierarchy)
            {
                currentContent.ProcessExit();
            }

            UpdateTooltipPosition();
            UpdateTooltipPivot();
        }

        private void UpdateTooltipPosition()
        {
#if ENABLE_LEGACY_INPUT_MANAGER
            Vector3 cursorPos = Input.mousePosition;
#elif ENABLE_INPUT_SYSTEM
            cursorPos = Mouse.current.position.ReadValue();
#endif

            //CheckForBounds();

            if (mainCanvas.renderMode == RenderMode.ScreenSpaceCamera || mainCanvas.renderMode == RenderMode.WorldSpace)
            {
                RectTransformUtility.ScreenPointToLocalPointInRectangle(currentTooltip.transform.parent.GetComponent<RectTransform>(), cursorPos, targetCamera, out Vector2 outPoint);
                currentTooltip.transform.localPosition = outPoint;

                //if (transitionMode == TransitionMode.Damp) { tooltipRect.tooltipContent.localPosition = Vector3.SmoothDamp(tooltipRect.tooltipContent.localPosition, contentPos, ref tooltipVelocity, tooltipSmoothness, dampSpeed * 1000, Time.unscaledDeltaTime); }
                //else { tooltipRect.tooltipContent.localPosition = contentPos; }
            }

            else if (mainCanvas.renderMode == RenderMode.ScreenSpaceOverlay)
            {
                currentTooltip.transform.position = cursorPos;

                //if (transitionMode == TransitionMode.Damp) { tooltipRect.tooltipContent.position = Vector3.SmoothDamp(tooltipRect.tooltipContent.position, cursorPos + contentPos, ref tooltipVelocity, tooltipSmoothness, dampSpeed * 1000, Time.unscaledDeltaTime); }
                //else { tooltipRect.tooltipContent.position = cursorPos + contentPos; }
            }
        }

        private void UpdateTooltipPivot()
        {
            currentTooltip.rect.pivot = currentTooltip.tooltipContent.pivot;

            if (currentTooltip.rect.offsetMax.x > sourceRect.rect.width)
                currentTooltip.rect.pivot = currentTooltip.rect.pivot * Vector2.up + Vector2.right;

            if (currentTooltip.rect.offsetMin.x < 0)
                currentTooltip.rect.pivot *= Vector2.up;

            if (currentTooltip.rect.offsetMax.y > sourceRect.rect.height)
                currentTooltip.rect.pivot = currentTooltip.rect.pivot * Vector2.right + Vector2.up;

            if (currentTooltip.rect.offsetMin.y < 0)
                currentTooltip.rect.pivot *= Vector2.right;
        }

        /* =======================from TooltipContent============================== */

        private Sequence _enterTween;
        public void ProcessEnter(Tooltip tooltip, TooltipContent content)
        {
            _enterTween = DOTween.Sequence();
            _enterTween.AppendInterval(content.delay);
            _enterTween.AppendCallback(() => ProcessEnterInternal(tooltip, content));
            _enterTween.Play();
        }

        private void ProcessEnterInternal(Tooltip tooltip, TooltipContent content)
        {
            currentTooltip = tooltip;
            allowUpdate = true;
            currentContent = content;
            currentTooltip.ProcessEnter(content);
        }

        public void ProcessExit(Tooltip tooltip)
        {
            _enterTween?.Complete();
            tooltip.ProcessExit();
            allowUpdate = false;
        }
    }
}