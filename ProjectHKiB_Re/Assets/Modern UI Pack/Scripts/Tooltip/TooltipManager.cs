using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;


#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace Michsky.MUIP
{
    public class TooltipManager : MonoBehaviour
    {
        // Resources
        public Canvas mainCanvas;
        public RectTransform tooltipRect;
        public RectTransform tooltipContent;
        public RectTransform iconsParent;
        public Camera targetCamera;
        private TextMeshProUGUI descriptionText;

        // Settings
        [Range(0.01f, 0.5f)] public float tooltipSmoothness = 0.1f;
        [Range(5, 10)] public float dampSpeed = 10;
        public float preferredWidth = 375;
        public bool allowUpdate = true;
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

        [HideInInspector] public LayoutElement contentLE;
        [HideInInspector] public TooltipContent currentTooltip;
        [HideInInspector] public Animator tooltipAnimator;
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
            descriptionText = tooltipRect.GetComponentInChildren<TextMeshProUGUI>();

            sourceRect.anchorMin = new Vector2(0, 0);
            sourceRect.anchorMax = new Vector2(1, 1);
            sourceRect.offsetMin = new Vector2(0, 0);
            sourceRect.offsetMax = new Vector2(0, 0);

            tooltipContent.pivot = new Vector2(0f, tooltipContent.pivot.y);
            tooltipContent.pivot = new Vector2(tooltipContent.pivot.x, 0f);

            if (mainCanvas == null) { mainCanvas = gameObject.GetComponentInParent<Canvas>(); }
            if (cameraSource == CameraSource.Main) { targetCamera = Camera.main; }

            contentPos = new Vector3(vBorderTop, hBorderLeft, 0);
            gameObject.transform.SetAsLastSibling();

            tooltipAnimator = tooltipRect.GetComponentInParent<Animator>();
            if (contentLE == null)
                contentLE = descriptionText.GetComponent<LayoutElement>();
        }

        void Update()
        {
            if (!allowUpdate) { return; }
            if (checkDispose && currentTooltip != null && !currentTooltip.gameObject.activeInHierarchy) { currentTooltip.ProcessExit(); }

            CheckForPosition();
        }

        void CheckForPosition()
        {
#if ENABLE_LEGACY_INPUT_MANAGER
            Vector3 cursorPos = Input.mousePosition;
#elif ENABLE_INPUT_SYSTEM
            cursorPos = Mouse.current.position.ReadValue();
#endif

            //CheckForBounds();

            if (mainCanvas.renderMode == RenderMode.ScreenSpaceCamera || mainCanvas.renderMode == RenderMode.WorldSpace)
            {
                RectTransformUtility.ScreenPointToLocalPointInRectangle(tooltipRect.parent.GetComponent<RectTransform>(), cursorPos, targetCamera, out Vector2 outPoint);
                tooltipRect.localPosition = outPoint;

                if (transitionMode == TransitionMode.Damp) { tooltipContent.transform.localPosition = Vector3.SmoothDamp(tooltipContent.transform.localPosition, contentPos, ref tooltipVelocity, tooltipSmoothness, dampSpeed * 1000, Time.unscaledDeltaTime); }
                else { tooltipContent.transform.localPosition = contentPos; }
            }

            else if (mainCanvas.renderMode == RenderMode.ScreenSpaceOverlay)
            {
                tooltipRect.position = cursorPos;

                if (transitionMode == TransitionMode.Damp) { tooltipContent.transform.position = Vector3.SmoothDamp(tooltipContent.transform.position, cursorPos + contentPos, ref tooltipVelocity, tooltipSmoothness, dampSpeed * 1000, Time.unscaledDeltaTime); }
                else { tooltipContent.transform.position = cursorPos + contentPos; }
            }
        }


        /*
        void CheckForBounds()
        {
            Vector2 uiPos = tooltipRect.anchoredPosition;
            if (uiPos.x <= xLeft)
            {
                contentPos = new Vector3(hBorderLeft, contentPos.y, 0);
                tooltipContent.pivot = new Vector2(0f, tooltipContent.pivot.y);
            }
            else if (uiPos.x >= xRight)
            {
                contentPos = new Vector3(hBorderRight, contentPos.y, 0);
                tooltipContent.pivot = new Vector2(1f, tooltipContent.pivot.y);
            }
            if (uiPos.y <= yTop)
            {
                contentPos = new Vector3(contentPos.x, vBorderBottom, 0);
                tooltipContent.pivot = new Vector2(tooltipContent.pivot.x, 0f);
            }
            else if (uiPos.y >= yBottom)
            {
                contentPos = new Vector3(contentPos.x, vBorderTop, 0);
                tooltipContent.pivot = new Vector2(tooltipContent.pivot.x, 1f);
            }
        }
        */

        /* =======================from TooltipContent============================== */
        private static readonly WaitForSecondsRealtime _waitForSecondsRealtime0_05 = new(0.05f);
        public void ProcessEnter(TooltipContent tooltip)
        {
            if (tooltipRect == null)
                return;

            descriptionText.text = tooltip.description;

            allowUpdate = true;
            currentTooltip = tooltip;

            CheckForContentWidth();


            tooltipAnimator.gameObject.SetActive(false);
            tooltipAnimator.gameObject.SetActive(true);

            if (tooltip.delay == 0) { tooltipAnimator.Play("In"); }
            else { StartCoroutine(ShowTooltip(tooltip.delay)); }

            if (tooltip.forceToUpdate == true)
                StartCoroutine(nameof(UpdateLayoutPosition));

            Image[] icons = iconsParent.GetComponentsInChildren<Image>();
            for (int i = 0; i < icons.Length; i++)
            {
                if (tooltip.icons.Length > i)
                {
                    icons[i].gameObject.SetActive(true);
                    icons[i].sprite = tooltip.icons[i];
                }
                else
                {
                    icons[i].gameObject.SetActive(false);
                }
            }
        }

        public void ProcessExit(TooltipContent tooltip)
        {
            if (tooltipRect == null)
                return;

            if (tooltip.delay != 0)
            {
                StopCoroutine(nameof(ShowTooltip));

                if (tooltipAnimator.GetCurrentAnimatorStateInfo(0).IsName("In"))
                    tooltipAnimator.Play("Out");
            }

            else { tooltipAnimator.Play("Out"); }

            allowUpdate = false;
        }

        public void CheckForContentWidth() { LayoutElementCreator(); StartCoroutine(nameof(CalculateContentWidth)); }

        private void LayoutElementCreator()
        {
            if (contentLE == null)
            {
                descriptionText.gameObject.AddComponent<LayoutElement>();
                contentLE = descriptionText.GetComponent<LayoutElement>();
            }

            contentLE.preferredWidth = preferredWidth;
            contentLE.enabled = false;
        }

        IEnumerator CalculateContentWidth()
        {
            yield return _waitForSecondsRealtime0_05;
            float tempWidth = descriptionText.GetComponent<RectTransform>().sizeDelta.x;

            if (tempWidth >= preferredWidth + 1)
                contentLE.enabled = true;

            LayoutRebuilder.ForceRebuildLayoutImmediate(contentLE.gameObject.GetComponent<RectTransform>());
            contentLE.preferredWidth = preferredWidth;
        }

        IEnumerator ShowTooltip(float delay)
        {
            yield return new WaitForSecondsRealtime(delay);
            tooltipAnimator.Play("In");
            StopCoroutine(nameof(ShowTooltip));
        }

        IEnumerator UpdateLayoutPosition()
        {
            yield return _waitForSecondsRealtime0_05;
            LayoutRebuilder.ForceRebuildLayoutImmediate(tooltipAnimator.gameObject.GetComponent<RectTransform>());
        }
    }
}