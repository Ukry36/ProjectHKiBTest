using UnityEngine;
using UnityEngine.EventSystems;

namespace Michsky.MUIP
{
    [AddComponentMenu("Modern UI Pack/Tooltip/Tooltip Content")]
    public class TooltipContent : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [Header("Content")]
        public Sprite[] icons;
        [TextArea] public string description;
        public float delay;

        [Header("Resources")]
        [SerializeField] protected TooltipManager tooltipManager;

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
            tooltipManager.ProcessEnter(this);
        }
        public virtual void ProcessExit() => tooltipManager.ProcessExit(this);

        public void OnPointerEnter(PointerEventData eventData) { ProcessEnter(); }
        public void OnPointerExit(PointerEventData eventData) { ProcessExit(); }

#if !UNITY_IOS && !UNITY_ANDROID
        public void OnMouseEnter() { if (useIn3D == true) { ProcessEnter(); } }
        public void OnMouseExit() { if (useIn3D == true) { ProcessExit(); } }
#endif
    }
}