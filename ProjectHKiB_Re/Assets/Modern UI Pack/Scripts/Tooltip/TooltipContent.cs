using UnityEngine;
using UnityEngine.EventSystems;

namespace Michsky.MUIP
{
    [AddComponentMenu("Modern UI Pack/Tooltip/Tooltip Content")]
    public class TooltipContent : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [Header("Content")]
        public Sprite image36x36;
        public Sprite icon9x9;
        public Sprite[] icon5x5s;
        [TextArea] public string description;
        public float delay;

        [Header("Resources")]
        [SerializeField] protected TooltipManager tooltipManager;
        [SerializeField] protected Tooltip tooltipLayout;

        [Header("Settings")]
        public bool forceToUpdate = false;
        public bool useIn3D = false;

        public void Awake()
        {
            if (tooltipManager == null) tooltipManager = FindObjectOfType<TooltipManager>();
        }

        public virtual void ProcessEnter()
        {
            StopCoroutine("DisableAnimator");
            tooltipManager.ProcessEnter(tooltipLayout, this);
        }
        public virtual void ProcessExit() => tooltipManager.ProcessExit(tooltipLayout);

        public void OnPointerEnter(PointerEventData eventData) { ProcessEnter(); }
        public void OnPointerExit(PointerEventData eventData) { ProcessExit(); }

#if !UNITY_IOS && !UNITY_ANDROID
        public void OnMouseEnter() { if (useIn3D == true) { ProcessEnter(); } }
        public void OnMouseExit() { if (useIn3D == true) { ProcessExit(); } }
#endif
    }
}