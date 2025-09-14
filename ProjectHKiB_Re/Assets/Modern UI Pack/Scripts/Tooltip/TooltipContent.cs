using System.Collections.Generic;
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
        public List<Sprite> icon5x5s;
        [TextArea] public string description;
        [NaughtyAttributes.MinValue(0.1f)] public float delay;

        [Header("Resources")]
        [SerializeField] protected TooltipManager tooltipManager;
        [SerializeField] protected Tooltip tooltipLayout;


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
    }
}