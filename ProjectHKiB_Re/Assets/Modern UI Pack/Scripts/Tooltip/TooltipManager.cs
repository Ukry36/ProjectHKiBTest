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

        // Border Bounds
        //[SerializeField] private int xLeft = -400;
        //[SerializeField] private int xRight = 400;
        //[SerializeField] private int yTop = -325;
        //[SerializeField] private int yBottom = 325;

        [HideInInspector] public TooltipContent currentContent;
        Vector3 contentPos = new Vector3(0, 0, 0);
        Vector3 tooltipVelocity = Vector3.zero;

        public enum CameraSource { Main, Custom }
        public enum TransitionMode { Damp, Snap }

        void Awake()
        {
            RectTransform sourceRect = gameObject.GetComponent<RectTransform>();

            if (sourceRect == null)
            {
                Debug.LogError("<b>[Tooltip]</b> Rect Transform is missing from the object.", this);
                return;
            }

            sourceRect.anchorMin = new Vector2(0, 0);
            sourceRect.anchorMax = new Vector2(1, 1);
            sourceRect.offsetMin = new Vector2(0, 0);
            sourceRect.offsetMax = new Vector2(0, 0);
            Tooltip[] tooltips = GetComponentsInChildren<Tooltip>();
            for (int i = 0; i < tooltips.Length; i++)
            {
                tooltips[i].tooltipContent.pivot = new Vector2(0f, tooltips[i].tooltipContent.pivot.y);
                tooltips[i].tooltipContent.pivot = new Vector2(tooltips[i].tooltipContent.pivot.x, 0f);
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
        }

        void UpdateTooltipPosition()
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
        /*
                void CheckForBounds()
                {
                    Vector2 uiPos = currentTooltip.tooltipRect.anchoredPosition;
                    if (uiPos.x <= xLeft)
                    {
                        contentPos = new Vector3(hBorderLeft, contentPos.y, 0);
                        currentTooltip.tooltipContent.pivot = new Vector2(0f, currentTooltip.tooltipContent.pivot.y);
                    }
                    else if (uiPos.x >= xRight)
                    {
                        contentPos = new Vector3(hBorderRight, contentPos.y, 0);
                        currentTooltip.tooltipContent.pivot = new Vector2(1f, currentTooltip.tooltipContent.pivot.y);
                    }
                    if (uiPos.y <= yTop)
                    {
                        contentPos = new Vector3(contentPos.x, vBorderBottom, 0);
                        currentTooltip.tooltipContent.pivot = new Vector2(currentTooltip.tooltipContent.pivot.x, 0f);
                    }
                    else if (uiPos.y >= yBottom)
                    {
                        contentPos = new Vector3(contentPos.x, vBorderTop, 0);
                        currentTooltip.tooltipContent.pivot = new Vector2(currentTooltip.tooltipContent.pivot.x, 1f);
                    }
                }*/


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